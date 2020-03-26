using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Registration.ReadModel;
using Registration.ReadModel.Implementation;

namespace Conference.Public.Api
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
            services.AddControllers();
            
            services.AddMemoryCache();
            services.AddCors(options =>
                options.AddPolicy("Open", builder => builder.AllowAnyOrigin().AllowAnyHeader()));

            services.AddControllers()
                .AddNewtonsoftJson(options =>
                {
                    options.SerializerSettings.ContractResolver = new DefaultContractResolver();
                    options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Error;
                });


            services.AddSingleton<Func<ConferenceRegistrationDbContext>>(x => () => {
                var optionsBuilder = new DbContextOptionsBuilder();
                optionsBuilder.UseSqlServer(Configuration.GetConnectionString("ConferenceReadDbContext"));

                return new ConferenceRegistrationDbContext(optionsBuilder.Options);
            });


            services.AddSingleton<IConferenceDao, CachingConferenceDao>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseCors("Open");

            app.UseEndpoints(endpoints => { endpoints.MapControllers(); });
        }
    }
}