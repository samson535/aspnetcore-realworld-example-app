using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;

namespace Conduit
{
    public class Program
    {
        public static int Main(string[] args)
        {
            // read database configuration (database provider + database connection) from environment variables
            IConfiguration config = new ConfigurationBuilder()
                .AddEnvironmentVariables()
                .Build();

            //var host = new WebHostBuilder()
            //    .UseConfiguration(config)
            //    .UseKestrel()
            //    .UseUrls($"http://+:5000")
            //    .UseStartup<Startup>()
            //    .Build();
            //host.Run();

            // Create the Serilog logger, and configure the sinks
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .WriteTo.Seq("http://localhost:5341")
                .CreateLogger();

            // Wrap creating and running the host in a try-catch block
            try
            {
                Log.Information("Starting host");
                CreateHostBuilder(args, config)
                    .Build().Run();
                return 0;
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Host terminated unexpectedly");
                return 1;
            }
            finally
            {
                Log.CloseAndFlush();
            }

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public static IHostBuilder CreateHostBuilder(string[] args, IConfiguration config) =>
            new HostBuilder()
                .UseSerilog()
                .ConfigureHostConfiguration(builder =>
                    { /* Host configuration */ })
                .ConfigureAppConfiguration(builder =>
                    { /* App configuration */ })
                .ConfigureServices(services =>
                    { /* Service configuration */})
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseKestrel()
                    .UseConfiguration(config)
                    .UseUrls($"http://+:5000")
                    .UseStartup<Startup>();

                }); 
      
    }
}
