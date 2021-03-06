﻿using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace Tweezers.Api.Middleware
{
    public class TweezersMiddleware
    {
        public static void AddIgnoreNullService(IServiceCollection services)
        {
            services.AddMvc()
                .AddJsonOptions(options => {
                    options.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;
                });
        }
    }
}
