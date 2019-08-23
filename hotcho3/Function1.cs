
using System.Security.Claims;
using HotChocolate;
using HotChocolate.AspNetCore;
using HotChocolate.AspNetCore.Voyager;
using HotChocolate.Execution.Configuration;
using HotChocolate.Subscriptions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using StarWars.Data;
using StarWars.Types;

using System;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;

using HotChocolate.AspNetCore.Playground;


using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting.Server;

using Microsoft.AspNetCore.Hosting.Internal;
using Microsoft.AspNetCore.Http.Features;
using System.Threading;
using System.Net.Http;
using System.IO;

using Microsoft.Extensions.Logging;

[assembly: FunctionsStartup(typeof(StarWars.Startup))]

namespace StarWars
{

    public  class InternalServer : IServer
    {
        private bool _disposed = false;
        private IWebHost _host;

        public IHttpApplication<HostingApplication.Context> Application;

        public static InternalServer Instance { get; set; }

        static InternalServer()
        {
            var builder = new WebHostBuilder()
                .UseStartup<StarWars.Startup>();
            Instance = new InternalServer(builder);
        }

        public InternalServer(IWebHostBuilder builder)
            : this(builder, new FeatureCollection())
        {
        }

        public InternalServer(IWebHostBuilder builder, IFeatureCollection featureCollection)
        {
            var host = builder.UseServer(this).Build();
            host.StartAsync().GetAwaiter().GetResult();
            _host = host;
        }

        public IFeatureCollection Features { get; }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                _host.Dispose();
            }
        }

        public Task StartAsync<TContext>(IHttpApplication<TContext> application, CancellationToken cancellationToken)
        {
            this.Application = (IHttpApplication<HostingApplication.Context>)application;

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
    public class ProxyActionResult : IActionResult
    {
        public async Task ExecuteResultAsync(ActionContext context)
        {
            await InternalServer.Instance.Application.ProcessRequestAsync(new HostingApplication.Context() { HttpContext = context.HttpContext });
        }
    }
    public class Startup : IStartup //FunctionsStartup
    {

        private static void Handle1(IApplicationBuilder app)
        {
            app.Run(async context =>
            {
                await context.Response.WriteAsync("map test 1");
            });
        }
        public void Configure(IApplicationBuilder app)
        {
            //app                
            //    .UseGraphQL("/graphql")                
            //    .UsePlayground("/graphql")
            //    .UseVoyager("/graphql");

            app
                .UseGraphQL()
                .UsePlayground()
                .UseVoyager();

            //app.Map("/map1", Handle1);
            //var env = app.ApplicationServices.GetService<IHostingEnvironment>();
            //var loggerFactory = app.ApplicationServices.GetService<ILoggerFactory>();

            //app.UseGraphQL();

            //app.UseGraphQLHttpGet();
            //app.UseGraphQLHttpGetSchema();
            //app.UseGraphQLHttpPost();
            //app.UsePlayground("/playground");
            //app.UseGraphQL();
            //app.UseGraphQLHttpGetSchema();
            //app.Build();
            //.Run(async context =>
            //{
            //    await context.Response.WriteAsync("hello gav");
            //});

        }

        IServiceProvider IStartup.ConfigureServices(IServiceCollection services)
        {
            // Add the custom services like repositories etc ...
            services.AddSingleton<CharacterRepository>();
            services.AddSingleton<ReviewRepository>();

            // Add in-memory event provider
            //services.AddInMemorySubscriptionProvider();

            // Add GraphQL Services
            services.AddGraphQL(sp => SchemaBuilder.New()
                .AddServices(sp)

                // Adds the authorize directive and
                // enable the authorization middleware.
                .AddAuthorizeDirectiveType()

                .AddQueryType<QueryType>()
                .AddMutationType<MutationType>()
                .AddSubscriptionType<SubscriptionType>()
                .AddType<HumanType>()
                .AddType<DroidType>()
                .AddType<EpisodeType>()
                .Create(),
                new QueryExecutionOptions
                {
                    TracingPreference = TracingPreference.Always
                });


            // Add Authorization Policy
            //services.AddAuthorization(options =>
            //{
            //    options.AddPolicy("HasCountry", policy =>
            //        policy.RequireAssertion(context =>
            //            context.User.HasClaim(c =>
            //                (c.Type == ClaimTypes.Country))));
            //});

            /*
            Note: uncomment this
            section in order to simulate a user that has a country claim and
            passes the configured authorization rule.
            services.AddQueryRequestInterceptor((ctx, builder, ct) =>
            {
                var identity = new ClaimsIdentity("abc");
                identity.AddClaim(new Claim(ClaimTypes.Country, "us"));
                ctx.User = new ClaimsPrincipal(identity);
                builder.SetProperty(nameof(ClaimsPrincipal), ctx.User);
                return Task.CompletedTask;
            });
            */

            return services.BuildServiceProvider();
        }
    }

    public static class GraphQLPostFunction
    {
        [FunctionName("GraphQLPostFunction")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post","get", Route = null)]
            HttpRequest request)
        {
            // we need to call the HotChoc service, but how is that done?
            //return new ProxyActionResult();
            return new ProxyActionResult();

            //return new OkResult();
        }
    }
}
