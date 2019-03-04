using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using SyncService.HiDriveClient;
using SyncService.HiDriveClient.Authentication;
using SyncService.Options;
using SyncService.Services.Account;
using SyncService.Services.Folder;
using SyncService.Services.Sync;
using IHostingEnvironment = Microsoft.AspNetCore.Hosting.IHostingEnvironment;

namespace SyncService
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddOptions();
            services.AddDataProtection().ProtectKeysWithDpapi(protectToLocalMachine: true).PersistKeysToFileSystem(
                new DirectoryInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "SyncService")));
            services.Configure<HiDriveApiOptions>(Configuration);
            services.AddSingleton<IOptions<HostOptions>>(provider =>
                new OptionsWrapper<HostOptions>(new HostOptions {ShutdownTimeout = TimeSpan.FromSeconds(20)}));
            services.AddSingleton<AccountService>();
            services.AddSingleton<FolderConfigurationService>();
            services.AddSingleton<HiDriveSyncService>();
            services.AddSingleton<IHiDriveAuthenticator, HiDriveAuthenticator>(provider => new HiDriveAuthenticator(provider.GetRequiredService<IOptions<HiDriveApiOptions>>().Value.HiDriveClientId, provider.GetRequiredService<IOptions<HiDriveApiOptions>>().Value.HiDriveClientSecret));
            services.AddSingleton<IHiDriveClient, HiDriveClient.HiDriveClient>();
            services.AddSingleton<IHostedService>(provider => provider.GetRequiredService<HiDriveSyncService>());
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1)
                .AddFluentValidation(configuration =>
                    configuration.RegisterValidatorsFromAssemblyContaining<Startup>());
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
            }

            app.UseStaticFiles();

            app.UseMvc();
        }
    }
}
