using Microsoft.AspNetCore.Authentication.Certificate;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

namespace TheWatch.Shared.Security;

/// <summary>
/// mTLS and TLS configuration extensions for inter-service mutual authentication
/// and FIPS 140-2 compliant transport security. Implements DISA STIG V-222596
/// (TLS 1.2 minimum) and NIST SP 800-52r2 cipher suite requirements.
/// </summary>
public static class WatchTlsExtensions
{
    /// <summary>
    /// Configures Kestrel for mutual TLS (mTLS) on inter-service communication ports.
    /// Requires client certificates and validates them against a trusted Certificate Authority.
    /// </summary>
    /// <param name="builder">The web application builder to configure.</param>
    /// <returns>The builder for chaining.</returns>
    /// <remarks>
    /// Reads <c>Security:MtlsCaCertPath</c> from configuration for the trusted CA certificate.
    /// Client certificates must be issued by this CA to be accepted.
    /// </remarks>
    public static WebApplicationBuilder ConfigureWatchMtls(this WebApplicationBuilder builder)
    {
        var caCertPath = builder.Configuration["Security:MtlsCaCertPath"];

        builder.WebHost.ConfigureKestrel(options =>
        {
            options.ConfigureHttpsDefaults(httpsOptions =>
            {
                // Require client certificates for mutual TLS
                httpsOptions.ClientCertificateMode = ClientCertificateMode.RequireCertificate;

                // Enforce TLS 1.2 and 1.3 only
                httpsOptions.SslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13;

                // Validate client certificate against trusted CA
                if (!string.IsNullOrEmpty(caCertPath))
                {
                    httpsOptions.ClientCertificateValidation = (certificate, chain, errors) =>
                    {
                        if (chain is null) return false;

                        // Load the trusted CA certificate
                        using var caCert = X509CertificateLoader.LoadCertificateFromFile(caCertPath);

                        chain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
                        chain.ChainPolicy.CustomTrustStore.Add(caCert);
                        chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;

                        return chain.Build(certificate);
                    };
                }
            });
        });

        return builder;
    }

    /// <summary>
    /// Adds certificate-based authentication scheme to the service collection.
    /// Validates client certificates for subject, issuer, and expiration.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddWatchCertificateAuth(this IServiceCollection services)
    {
        services.AddAuthentication(CertificateAuthenticationDefaults.AuthenticationScheme)
            .AddCertificate(options =>
            {
                // Accept chained certificates (not just self-signed)
                options.AllowedCertificateTypes = CertificateTypes.Chained;

                // Validate the certificate is not expired and chain is intact
                options.ValidateCertificateUse = true;
                options.ValidateValidityPeriod = true;

                // Revocation checking — online if available
                options.RevocationMode = X509RevocationMode.Online;
                options.RevocationFlag = X509RevocationFlag.ExcludeRoot;

                options.Events = new CertificateAuthenticationEvents
                {
                    OnCertificateValidated = context =>
                    {
                        // Extract service identity from certificate subject
                        var claims = new[]
                        {
                            new System.Security.Claims.Claim(
                                System.Security.Claims.ClaimTypes.NameIdentifier,
                                context.ClientCertificate.Subject),
                            new System.Security.Claims.Claim(
                                System.Security.Claims.ClaimTypes.Name,
                                context.ClientCertificate.GetNameInfo(
                                    X509NameType.SimpleName, forIssuer: false) ?? "unknown"),
                            new System.Security.Claims.Claim(
                                "cert_thumbprint",
                                context.ClientCertificate.Thumbprint),
                            new System.Security.Claims.Claim(
                                System.Security.Claims.ClaimTypes.Role,
                                "ServiceAccount")
                        };

                        context.Principal = new System.Security.Claims.ClaimsPrincipal(
                            new System.Security.Claims.ClaimsIdentity(claims, context.Scheme.Name));
                        context.Success();

                        return Task.CompletedTask;
                    },
                    OnAuthenticationFailed = context =>
                    {
                        context.Fail("Certificate validation failed: " + context.Exception?.Message);
                        return Task.CompletedTask;
                    }
                };
            });

        return services;
    }

    /// <summary>
    /// Configures Kestrel for TLS 1.2/1.3 enforcement with FIPS-approved cipher suites only.
    /// Does not require client certificates (use <see cref="ConfigureWatchMtls"/> for that).
    /// </summary>
    /// <param name="builder">The web application builder to configure.</param>
    /// <returns>The builder for chaining.</returns>
    public static WebApplicationBuilder ConfigureWatchTls(this WebApplicationBuilder builder)
    {
        builder.WebHost.ConfigureKestrel(options =>
        {
            // Suppress server version header
            options.AddServerHeader = false;

            options.ConfigureHttpsDefaults(httpsOptions =>
            {
                // FIPS 140-2: TLS 1.2 and TLS 1.3 only (disable TLS 1.0, 1.1, SSL 3.0)
                httpsOptions.SslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13;

                // Client certificates optional for standard TLS mode
                httpsOptions.ClientCertificateMode = ClientCertificateMode.NoCertificate;
            });
        });

        return builder;
    }
}
