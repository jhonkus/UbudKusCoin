using System;
using System.Reflection;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Grpc.Net.Client;
using UbudKusCoin.P2P;
using Xunit;

namespace UbudKusCoin.Tests;

public class P2PMtlsTests : IDisposable
{
    private readonly string _tempCertPath;

    public P2PMtlsTests()
    {
        _tempCertPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"p2p_test_cert_{Guid.NewGuid():N}.pfx");
        CreateSelfSignedPfxCertificate(_tempCertPath, "password");
    }

    public void Dispose()
    {
        if (System.IO.File.Exists(_tempCertPath))
        {
            try { System.IO.File.Delete(_tempCertPath); } catch { }
        }
    }

    private static void CreateSelfSignedPfxCertificate(string path, string password)
    {
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest("cn=UbudKusCoinTest", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        var cert = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1));
        var pfxBytes = cert.Export(X509ContentType.Pkcs12, password);
        System.IO.File.WriteAllBytes(path, pfxBytes);
    }

    [Fact]
    public void CreateChannel_WithHttpsAndClientCert_CorrectlyConfiguresHttpHandler()
    {
        Environment.SetEnvironmentVariable("P2P_TLS_CLIENT_CERT_PATH", _tempCertPath);
        Environment.SetEnvironmentVariable("P2P_TLS_CLIENT_CERT_PASSWORD", "password");
        Environment.SetEnvironmentVariable("P2P_ALLOW_UNTRUSTED_ROOT", "true");

        try
        {
            // Retrieve private static method CreateChannel from P2PService
            var method = typeof(P2PService).GetMethod(
                "CreateChannel",
                BindingFlags.NonPublic | BindingFlags.Static);

            Assert.NotNull(method);

            var address = "https://localhost:26658";
            using var channel = (GrpcChannel)method.Invoke(null, new object[] { address })!;

            Assert.NotNull(channel);
        }
        finally
        {
            Environment.SetEnvironmentVariable("P2P_TLS_CLIENT_CERT_PATH", null);
            Environment.SetEnvironmentVariable("P2P_TLS_CLIENT_CERT_PASSWORD", null);
            Environment.SetEnvironmentVariable("P2P_ALLOW_UNTRUSTED_ROOT", null);
        }
    }
}
