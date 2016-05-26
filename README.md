Sezame C# SDK
=======

Passwordless multi-factor authentication. 

Unlike password-based solutions that require you to remember just another PIN or password, sezame is  a secure and simple multi-factor authentication solution. You only need the username and your fingerprint on your smartphone to log into any sezame-enabled site. Magic – Sezame – ENTER SIMPLICITY!.

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

```c#  
var invoker = new SezameRegistrationServiceInvoker();
var response = await invoker.RegisterAsync(email, applicationName);
clientcode = response.GetParameter(SezameResultKey.ClientCode);
sharedsecret = response.GetParameter(SezameResultKey.SharedSecret);
```

### sign

After you have authorized the registration on your mobile device you can request the certificate.

```php

```
Store the certificate and the private key within your system, it is recommended to protect your
private key with a secure passphrase.
The certificate and the private key is needed for subsequent calls to the Sezame servers, sign
and register are the only two calls which can be used without the client certificate.

### pair

Once you have successfully obtained the client certificate, let your customers pair their devices
with your application, this is done by displaying a QR code which is read by the Sezame app.

```php

```

### auth

To authenticate users with Sezame, use the auth call.

```php


```

### fraud

It is possible to inform users about fraud attempts, this request could be send, if the user logs in
using the password.

```php


```

### cancel

To disable the service use the cancel call, no further requests will be accepted by the Sezame
servers:

```php

```

### error handling

The Sezame Lib throws exceptions in the case of an error.

```php


```


## License

This bundle is under the BSD license. For the full copyright and license
information please view the LICENSE file that was distributed with this source code.
