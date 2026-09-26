using System.Net;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;


namespace Wheelhouse.Api.Configurations;

/// <summary>Registers persistent cookie keys and explicitly trusted deployment proxies.</summary>
public static class DeploymentHosting
{
    /// <summary>Applies host-owned deployment settings without trusting arbitrary forwarded headers.</summary>
    public static WebApplicationBuilder AddDeploymentHosting(this WebApplicationBuilder builder)
    {
        var keyPath = builder.Configuration["DataProtection:KeyPath"];
        if (!string.IsNullOrWhiteSpace(keyPath))
            builder.Services.AddDataProtection().SetApplicationName("Wheelhouse")
                .PersistKeysToFileSystem(new DirectoryInfo(keyPath));

        var proxies = builder.Configuration.GetSection("Deployment:TrustedProxies").Get<string[]>() ?? [];
        builder.Services.PostConfigure<ForwardedHeadersOptions>(options =>
        {
            options.KnownIPNetworks.Clear();
            options.KnownProxies.Clear();
            options.KnownProxies.Add(IPAddress.Loopback);
            options.KnownProxies.Add(IPAddress.IPv6Loopback);
            foreach (var proxy in proxies)
                options.KnownProxies.Add(IPAddress.Parse(proxy));
            options.ForwardLimit = 1;
        });
        return builder;
    }
}
