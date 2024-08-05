using System.Security.Cryptography.X509Certificates;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;

namespace Host;

public static class WebHostBuilderExtensions
{
    public static IWebHostBuilder UseKeyVaultCert(this IWebHostBuilder builder, Uri uri)
    {
        Console.WriteLine("UseKeyVaultCert: {0}", uri);
        var secret = uri.Segments[2].TrimEnd('/');
        var client = new SecretClient(new Uri($"{uri.Scheme}://{uri.Host}"), new DefaultAzureCredential());
        X509Certificate2? cert;
        try
        {
            var response = client.GetSecret(secret);
            if (!response.HasValue) return builder;
            cert = new X509Certificate2(Convert.FromBase64String(response.Value.Value));
            Console.WriteLine("Loaded certificate from KeyVault");
        }
        catch (AuthenticationFailedException e)
        {
            Console.WriteLine(e.Message);
            return builder;
        }
        builder.ConfigureKestrel(options =>
        {
            options.ConfigureHttpsDefaults(httpsOptions =>
            {
                httpsOptions.ServerCertificate = cert;
            });
        });
        return builder;
    }
}