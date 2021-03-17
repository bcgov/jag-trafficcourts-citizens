﻿using Microsoft.Extensions.DependencyInjection;

namespace DisputeApi.Web.Features.Disputes.Configuration
{
    /// <summary>
    /// Extension to inject dispute configuration service in service collection
    /// </summary>
    public static class DisputeServiceConfigurationExtension
    {
        public static void AddDisputeService(this IServiceCollection collection)
        {
            collection.AddTransient<IDisputeService, DisputeService>();
        }
    }
}
