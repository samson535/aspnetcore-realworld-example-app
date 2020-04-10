using System;
using System.IO;
using System.Threading.Tasks;
using Conduit.Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace Conduit.IntegrationTests
{
    public class SliceFixture : IDisposable
    {
        static readonly IConfiguration Config;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ServiceProvider _provider;
        // local database for testing
        private readonly string DbName = Guid.NewGuid() + ".db";


        /// Static function setup configuration
        ///
        /// - Add environment variable
        /// </summary>
        static SliceFixture()
        {
            Config = new ConfigurationBuilder()
               .AddEnvironmentVariables()
               .SetBasePath(Directory.GetCurrentDirectory())
               .AddJsonFile("appsettings.json")
               .Build();
        }

        /// <summary> 
        /// Constructor
        ///
        /// - Configure logger        
        /// - Create in memory database
        /// - Add the database services as singleton
        /// - Create server and services
        /// </summary>
        public SliceFixture()
        {

            // configure logger
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(Config)
                .CreateLogger();


            Log.Information("--> SliceFixture()");


            // configure database
            DbContextOptionsBuilder builder = new DbContextOptionsBuilder();
            builder.UseInMemoryDatabase(DbName);

            // create services for the database
            var services = new ServiceCollection();
            services.AddSingleton(new ConduitContext(builder.Options));

            // configure the app
            var startup = new Startup(Config);
            startup.ConfigureServices(services);

            // configure provider for the database context
            _provider = services.BuildServiceProvider();

            // configure factory scope to execute the command
            _scopeFactory = _provider.GetService<IServiceScopeFactory>();

            // load database
            GetDbContext().Database.EnsureCreated();


            //Log.Logger = new LoggerConfiguration()
            //  .WriteTo.Console()
            //  .CreateLogger();
        }

        /// <summary>
        /// Database context
        /// 
        /// </summary>
        /// <returns></returns>
        public ConduitContext GetDbContext()
        {
            return _provider.GetRequiredService<ConduitContext>();
        }

        /// <summary>
        /// Ensure to commit changed in database and logger
        ///
        /// - Delete the database
        /// </summary>
        public void Dispose()
        {
            Log.Information("--> Dispose()");
            File.Delete(DbName);
            Log.CloseAndFlush();
        }

        /// <summary>
        ///
        /// Send request command with data and execute scope
        /// 
        /// </summary>
        /// <typeparam name="TResponse"></typeparam>
        /// <param name="request"></param>
        /// <returns></returns>
        public Task<TResponse> SendAsync<TResponse>(IRequest<TResponse> request)
        {
            Log.Information("--> Task<TResponse> SendAsync<TResponse>(IRequest<TResponse> request)");

            return ExecuteScopeAsync(sp =>
            {
                var mediator = sp.GetService<IMediator>();

                return mediator.Send(request);
            });
        }

        /// <summary>
        /// 
        /// Send the request to the mediator
        /// 
        /// </summary>
        /// <param name="request"></param>
        /// <returns>mediator </returns>
        public Task SendAsync(IRequest request)
        {

            Log.Information("--> Task SendAsync(IRequest request)");

            return ExecuteScopeAsync(sp =>
            {
                var mediator = sp.GetService<IMediator>();

                return mediator.Send(request);
            });
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="entities"></param>
        /// <returns></returns>
        public Task InsertAsync(params object[] entities)
        {
            Log.Information("--> Task InsertAsync(params object[] entities)");

            return ExecuteDbContextAsync(db =>
            {
                foreach (var entity in entities)
                {
                    db.Add(entity);
                }
                return db.SaveChangesAsync();
            });
        }


        ///---------------------------------------------------------
        ///  Execute database command
        ///---------------------------------------------------------

        /// <summary>
        /// 
        /// 
        /// </summary>
        /// <param name="action"></param>
        /// <returns></returns>
        //public Task ExecuteDbContextAsync(Func<ConduitContext, Task> action)
        //{
        //    Log.Information("--> Task ExecuteDbContextAsync(Func<ConduitContext, Task> action)");

        //    return ExecuteScopeAsync(sp => action(sp.GetService<ConduitContext>()));
        //}

        /// <summary>
        /// Execute database action 
        /// 
        /// </summary>
        /// <typeparam name="T">Target command type</typeparam>
        /// <param name="action">database function</param>
        /// <returns></returns>
        public Task<T> ExecuteDbContextAsync<T>(Func<ConduitContext, Task<T>> action)
        {
            Log.Information("--> Task<T> ExecuteDbContextAsync<T>(Func<ConduitContext, Task<T>> action)");

            return ExecuteScopeAsync(sp => action(sp.GetService<ConduitContext>()));
        }

        ///---------------------------------------------------------
        ///  Execute command
        ///---------------------------------------------------------

        /// <summary>
        /// Execute action
        /// 
        /// </summary>
        /// <param name="action"></param>
        /// <returns></returns>
        //public async Task ExecuteScopeAsync(Func<IServiceProvider, Task> action)
        //{
        //    Log.Information("--> Task ExecuteScopeAsync(Func<IServiceProvider, Task> action)");

        //    using (var scope = _scopeFactory.CreateScope())
        //    {
        //        await action(scope.ServiceProvider);
        //    }
        //}

        /// <summary>
        /// Execute action and return type
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="action"></param>
        /// <returns></returns>
        public async Task<T> ExecuteScopeAsync<T>(Func<IServiceProvider, Task<T>> action)
        {
            Log.Information("--> Task<T> ExecuteScopeAsync<T>(Func<IServiceProvider, Task<T>> action)");

            using (var scope = _scopeFactory.CreateScope())
            {
                return await action(scope.ServiceProvider);
            }
        }

    }
}