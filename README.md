Sezame C# SDK
=======

Passwordless multi-factor authentication. 

Unlike password-based solutions that require you to remember just another PIN or password, sezame is  a secure and simple multi-factor authentication solution. You only need the username and your fingerprint on your smartphone to log into any sezame-enabled site. Magic – [Sezame](https://seza.me/) – ENTER SIMPLICITY!.

## Steps

To be able to use Sezame within your application you have to fullfill these steps:

1. download and install the Sezame app from an app store
2. follow the registration process in the app
3. register your application/client
4. obtain a SSL client certificate
5. let your users pair their devices with your application
6. issue authentication requests

If you don not have a supported device with fingerprint reader, you must obtain the ssl certificate by
using the support channels of Sezame.

## Usage

### register

To be able to connect to the Sezame HQ server, you have to register your client/application, this is
done by sending the register call using your recovery e-mail entered during the app installation
process.
You'll get an authentication request on your Sezame app, which must be authorized.
You'll get back a clientcode this is the identifiert for your application.

```c#
var email = "your recovery email";
var applicationName = "my dotnet app";
var invoker = new SezameRegistrationServiceInvoker();
var response = await invoker.RegisterAsync(email, applicationName);
clientcode = response.GetParameter(SezameResultKey.ClientCode);
sharedsecret = response.GetParameter(SezameResultKey.SharedSecret);
```

### sign

After you have authorized the registration on your mobile device you can request the certificate, yout have to build a certificate signing request containing the clientcode as CN:

```c#
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
```

after building the CSR send it to the sezame HQ server, you will get a certificate back, you have to store the certificate together with your private key in the windows certificate store. You have to convert the BounceCastle private key to a RSACryptoServiceProvider. This part is tricky!!

```c#
var invoker = new SezameRegistrationServiceInvoker();
var response = await invoker.SignAsync(pemCertificationRequest, sharedsecret);
pemCertificate = response.GetParameter(SezameResultKey.Certificate);

pemCertificate = Regex.Replace(pemCertificate, "-----BEGIN CERTIFICATE-----", "");
pemCertificate = Regex.Replace(pemCertificate, "-----END CERTIFICATE-----", "");
var certificateByteData = Convert.FromBase64String(pemCertificate);

AsymmetricKeyParameter privateKey = keyPair.Private;

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
```

In this example the certificate is store in within the CurrentUser context, it is possible to use the LocalMachine context, but you have to grant access rights to you application, or run your application as administrator.

You could also save the certificate and private key into a .p12 certstore file.

### pair

Once you have successfully obtained the client certificate, let your customers pair their devices
with your application, this is done by displaying a QR code which is read by the Sezame app.

```c#
var username = "my application username";
var webRequestHandler = new WebRequestHandler();
webRequestHandler.ClientCertificates.Add(certificate); // X509Certificate
var invoker = new SezameAuthenticationServiceInvoker(webRequestHandler, true);

var linkResponse = await invoker.LinkAsync(username);
var id = linkResponse.GetParameter(SezameResultKey.Id);
var clientcode = linkResponse.GetParameter(SezameResultKey.ClientCode);
```

with the username, id and clientcode build a qrcode (using ZXing and Newtonsoft.JSON):

```c#
var data = JsonConvert.SerializeObject(new
{
    id = id,
    username = username.Text,
    client = clientcode
});

var writer = new BarcodeWriter
{
    Format = BarcodeFormat.QR_CODE,
    Options = new EncodingOptions { Width = 500, Height = 500, Margin = 10 }
};

using (var bitmap = writer.Write(data))
{
    using (var stream = new MemoryStream())
    {
        bitmap.Save(stream, ImageFormat.Png);
        var img = Image.FromStream(stream);
    }
}
```

### auth

To authenticate users with Sezame, use the auth call.

```php
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

```


### cancel

To disable the service use the cancel call, no further requests will be accepted by the Sezame
servers:

```c#
var webRequestHandler = new WebRequestHandler();
webRequestHandler.ClientCertificates.Add(certificate); // X509Certificate
var invoker = new SezameRegistrationServiceInvoker(webRequestHandler, true);
await invoker.CancelAsync();

var store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
store.Open(OpenFlags.ReadWrite);
store.Remove(certificate);
store.Close();
```
This snippet also removes the certificate from the certificate store

### read the certificate from the store 

To be able to find your certificate in the certificate store use the clientcode as identifier:

```c#
var store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
store.Open(OpenFlags.ReadOnly);
foreach (var cert in store.Certificates)
{
    if (cert.GetNameInfo(X509NameType.DnsName, false) == clientcode)
    {
        certificate = cert;
        break;
    }
}
store.Close();
```

## License

This bundle is under the BSD license. For the full copyright and license
information please view the LICENSE file that was distributed with this source code.
