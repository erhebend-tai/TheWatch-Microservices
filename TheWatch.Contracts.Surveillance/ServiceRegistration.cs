// Copyright (c) 2018 Barton Milnor Mallory. All rights reserved.

using Microsoft.Extensions.DependencyInjection;

namespace TheWatch.Contracts.Surveillance;

public static class ServiceRegistration
{
    public static IHttpClientBuilder AddSurveillanceClient(this IServiceCollection services)
    {
        services.AddScoped<ISurveillanceClient>(sp =>
            new SurveillanceClient(sp.GetRequiredService<IHttpClientFactory>().CreateClient("Surveillance")));

        return services.AddHttpClient("Surveillance");
    }
}
