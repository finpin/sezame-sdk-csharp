using System;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using Sezame.Registration;
using Sezame.Authentication;
using System.Text.RegularExpressions;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Crypto.Parameters;

namespace Sezame
{
    public delegate void SezameRegisterCallbackType(string clientcode, string sharedsecret);
    public delegate void SezameSignCallbackType(X509Certificate2 certificate);
    public delegate void SezameLinkCallbackType(string id, string clientcode);
    public delegate void SezameCancelCallbackType();
    public delegate void SezameAuthCallbackType(SezameAuthenticationResultKey status);

    public class SezameManager
    {
        public string status { get; set; }

        public string email { get; set; }
        public string clientcode { get; set; }
        public string sharedsecret { get; set; }
        protected string pemCertificate;
        protected X509Certificate2 certificate;
        protected AsymmetricCipherKeyPair keyPair;
        protected IniFile ini;

        public SezameManager()
        {
            this.init();
        }

        public void init()
        {
            var dir = Directory.GetCurrentDirectory();
            ini = new IniFile(dir + "\\sezame.ini");
            status = readSetting("status");
            if (status.Length == 0)
            {
                status = "new";
                writeSetting("status", "new");
            }
            email = readSetting("email");
            clientcode = readSetting("clientcode");
            sharedsecret = readSetting("sharedsecret");
            readCertificate();
        }

        protected string readSetting(string key)
        {
            return ini.readValue("sezame", key);
        }

        protected void writeSetting(string key, string value)
        {
            ini.writeValue("sezame", key, value);
        }

        protected void readCertificate()
        {
            if (clientcode.Length == 0)
                return;

            var store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
            store.Open(OpenFlags.ReadOnly);
            foreach (var cert in store.Certificates)
            {
                if (cert.GetNameInfo(X509NameType.DnsName, false) == clientcode)
                {
                    certificate = cert;
                    status = "ready";
                    break;
                }
            }

            store.Close();
        }

        public async Task register(string email, string applicationName, SezameRegisterCallbackType callback)
        {
            var invoker = new SezameRegistrationServiceInvoker();
            var response = await invoker.RegisterAsync(email, applicationName);
            clientcode = response.GetParameter(SezameResultKey.ClientCode);
            sharedsecret = response.GetParameter(SezameResultKey.SharedSecret);
            this.email = email;
            writeSetting("status", "register");
            writeSetting("clientcode", clientcode);
            writeSetting("sharedsecret", sharedsecret);
            writeSetting("email", email);
            callback(clientcode, sharedsecret);
        }

        public string buildCsr()
        {
            string certparams = "CN=" + clientcode + ",E=" + email + ",C=AT,L=Vienna,ST=Austria,O=-,OU=-";
            X509Name name = new X509Name(certparams);

            RsaKeyPairGenerator rkpg = new RsaKeyPairGenerator();
            rkpg.Init(new KeyGenerationParameters(new SecureRandom(), 2048));
            keyPair = rkpg.GenerateKeyPair();

            Pkcs10CertificationRequest csr = new Pkcs10CertificationRequest("SHA512WITHRSA", name, keyPair.Public, null, keyPair.Private);

            StringBuilder stringBuilder = new StringBuilder();
            PemWriter premWriter = new PemWriter(new StringWriter(stringBuilder));
            premWriter.WriteObject(csr);
            premWriter.Writer.Flush();
            string pemCertificationRequest = stringBuilder.ToString();
            return pemCertificationRequest;
        }

        public async Task sign(string pemCertificationRequest, SezameSignCallbackType callback)
        {
            var invoker = new SezameRegistrationServiceInvoker();
            var response = await invoker.SignAsync(pemCertificationRequest, sharedsecret);
            pemCertificate = response.GetParameter(SezameResultKey.Certificate);

            pemCertificate = Regex.Replace(pemCertificate, "-----BEGIN CERTIFICATE-----", "");
            pemCertificate = Regex.Replace(pemCertificate, "-----END CERTIFICATE-----", "");
            var certificateByteData = Convert.FromBase64String(pemCertificate);

            AsymmetricKeyParameter privateKey = keyPair.Private;

            // http://paulstovell.com/blog/x509certificate2
            // Convert X509Certificate to X509Certificate2
            certificate = new X509Certificate2(certificateByteData, "test", X509KeyStorageFlags.Exportable);

            // Convert BouncyCastle Private Key to RSA
            var rsaPriv = DotNetUtilities.ToRSA((RsaPrivateCrtKeyParameters)keyPair.Private);

            // Setup RSACryptoServiceProvider with "KeyContainerName" set

            var csp = new CspParameters();
            csp.KeyContainerName = "KeyContainer";
            var rsaPrivate = new RSACryptoServiceProvider(csp);

            // Import private key from BouncyCastle's rsa
            rsaPrivate.ImportParameters(rsaPriv.ExportParameters(true));

            // Set private key on our X509Certificate2
            certificate.PrivateKey = rsaPrivate;

            var store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
            store.Open(OpenFlags.ReadWrite);
            store.Add(certificate);
            store.Close();
            
            status = "ready";

            callback(certificate);
        }

        public async Task link(string username, SezameLinkCallbackType callback)
        {
            var webRequestHandler = new WebRequestHandler();
            webRequestHandler.ClientCertificates.Add(certificate); // X509Certificate
            var invoker = new SezameAuthenticationServiceInvoker(webRequestHandler, true);

            var linkResponse = await invoker.LinkAsync(username);
            var id = linkResponse.GetParameter(SezameResultKey.Id);
            var clientcode = linkResponse.GetParameter(SezameResultKey.ClientCode);
            callback(id, clientcode);
        }

        public async Task auth(string username, string message, SezameAuthCallbackType callback)
        {
            var webRequestHandler = new WebRequestHandler();
            webRequestHandler.ClientCertificates.Add(certificate); // X509Certificate
            var invoker = new SezameAuthenticationServiceInvoker(webRequestHandler, true);

            SezameResult response = null;
            var timeout = TimeSpan.FromSeconds(60);
            response = await invoker.RequestAuthenticationAsync(username, message, "auth", (int)Math.Ceiling(timeout.TotalMinutes));
            var status = response.GetParameter(SezameResultKey.AuthenticationStatus);
            if (status == "notlinked")
            {
                callback(SezameAuthenticationResultKey.NotPaired);
                return;
            }

            var authId = response.GetParameter(SezameResultKey.Id);

            var result = SezameAuthenticationResultKey.Timedout;
            if (status == "initiated")
            {
                result = await Task.Run<SezameAuthenticationResultKey>(async () =>
                {
                    int sleeptime = 1000;
                    int loopPassCount = (int)Math.Ceiling(Math.Ceiling((double)timeout.TotalMilliseconds) / sleeptime);
                    while (loopPassCount > 0)
                    {
                        response = await invoker.CheckAuthenticationStatusAsync(authId);
                        status = response.GetParameter(SezameResultKey.AuthenticationStatus);
                        if (status == "authorized")
                        {
                            return SezameAuthenticationResultKey.Authenticated;
                        }
                        else if (status == "denied")
                        {
                            return SezameAuthenticationResultKey.Denied;
                        }
                        loopPassCount--;
                        System.Threading.Thread.Sleep(sleeptime);
                    }
                    return SezameAuthenticationResultKey.Timedout;
                });
            }

            callback(result);
        }

        public async Task cancel(SezameCancelCallbackType callback)
        {
            var webRequestHandler = new WebRequestHandler();
            webRequestHandler.ClientCertificates.Add(certificate); // X509Certificate
            var invoker = new SezameRegistrationServiceInvoker(webRequestHandler, true);
            await invoker.CancelAsync();

            var store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
            store.Open(OpenFlags.ReadWrite);
            store.Remove(certificate);
            store.Close();

            status = "new";
            clientcode = "";
            sharedsecret = "";
            email = "";
            writeSetting("status", status);
            writeSetting("clientcode", clientcode);
            writeSetting("sharedsecret", sharedsecret);
            writeSetting("email", email);

            callback();
        }
    }

    public class IniFile
    {
        public string path;

        [DllImport("kernel32")]
        private static extern long WritePrivateProfileString(string section,
            string key, string val, string filePath);
        [DllImport("kernel32")]
        private static extern int GetPrivateProfileString(string section,
                    string key, string def, StringBuilder retVal,
            int size, string filePath);

        public IniFile(string INIPath)
        {
            path = INIPath;
        }

        public void writeValue(string Section, string Key, string Value)
        {
            WritePrivateProfileString(Section, Key, Value, this.path);
        }

        public string readValue(string Section, string Key)
        {
            StringBuilder temp = new StringBuilder(255);
            int i = GetPrivateProfileString(Section, Key, "", temp, 255, this.path);
            return temp.ToString();
        }
    }
}


