# local.YARP
local reverse-proxy
## create local host certificate 

### Create a self-signed root certificate

```powershell
$params = @{
    Type = 'Custom'
    Subject = 'CN=MyLocalhostRootCert'
    KeySpec = 'Signature'
    KeyExportPolicy = 'Exportable'
    KeyUsage = 'CertSign'
    KeyUsageProperty = 'Sign'
    KeyLength = 2048
    HashAlgorithm = 'sha256'
    NotAfter = (Get-Date).AddMonths(24)
    CertStoreLocation = 'Cert:\CurrentUser\My'
}
$cert = New-SelfSignedCertificate @params
```

#### Export CA certificate
    * goto `run` and type `certmgr.msc`
    * goto `Manage user certificates -> Certificates - Current Users` 
    * goto `Personal -> Certificates`
    * Right click on the root cetificate and follow the Wizard and  export with private key.

#### Imoprt root certificate to tursted root
    * goto `Manage user certificates -> Certificates - Current Users`
    * goto `Trusted Root Certification Authorities -> Certificates`
    * right click on import and follow the Wizard
    * seelct `Trusted Root Certification Authorities` where necessary.

### Generate a client certificate with localhost

```powershell
$params = @{
       Type = 'Custom'
       Subject = 'CN=localhost'
       DnsName = 'localhost'
       KeySpec = 'Signature'
       KeyExportPolicy = 'Exportable'
       KeyLength = 2048
       HashAlgorithm = 'sha256'
       NotAfter = (Get-Date).AddMonths(18)
       CertStoreLocation = 'Cert:\CurrentUser\My'
       Signer = $cert
       TextExtension = @(
        '2.5.29.37={text}1.3.6.1.5.5.7.3.2')
   }
   New-SelfSignedCertificate @params
```


```powershell
$params = @{
       Type = 'Custom'
       Subject = 'CN=myhost'
       DnsName = 'myhost'
       KeySpec = 'Signature'
       KeyExportPolicy = 'Exportable'
       KeyLength = 2048
       HashAlgorithm = 'sha256'
       NotAfter = (Get-Date).AddMonths(18)
       CertStoreLocation = 'Cert:\CurrentUser\My'
       Signer = $cert
       TextExtension = @(
        '2.5.29.37={text}1.3.6.1.5.5.7.3.2')
   }
   New-SelfSignedCertificate @params
```

#### Export client/dns certificate
    * goto `run` and type `certmgr.msc`
    * goto `Manage user certificates -> Certificates - Current Users` 
    * goto `Personal -> Certificates`
    * Right click on the client/dns cetificate and follow the Wizard and  export with private key.


#### More info
    * How to Generate and export certificates for point-to-site using PowerShell](https://learn.microsoft.com/en-us/azure/vpn-gateway/vpn-gateway-certificates-point-to-site)
