using ConsolePubSubApp.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System;
using System.IO;
using System.Threading.Tasks;

namespace ConsolePubSubApp
{
    class Program
    {
        public static IConfiguration Configuration { get; private set; }

        public static async Task Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .WriteTo.File("app.log", rollingInterval: RollingInterval.Day)
                .CreateLogger();

            try
            {
                Configuration = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                    .Build();

                Environment.SetEnvironmentVariable(
                    "GOOGLE_APPLICATION_CREDENTIALS",
                    Configuration["GOOGLE_APPLICATION_CREDENTIALS"]);

                var services = new ServiceCollection();
                services.AddSingleton<IConfiguration>(Configuration);
                services.AddTransient<Publisher>();
                services.AddTransient<Subscriber>();

                var provider = services.BuildServiceProvider();
                await provider.GetRequiredService<Publisher>().Publish();
                await provider.GetRequiredService<Subscriber>().Subscription();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Unhandled exception in Main");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }
    }
}
