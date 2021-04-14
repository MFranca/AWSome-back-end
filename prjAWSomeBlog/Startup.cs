using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using prjS3Upload.Models;

namespace prjAWSomeBlog
{
    public class Startup
    {
        private readonly IConfiguration _config;
        
        public Startup(IConfiguration configuration)
        {
            _config = configuration; 
        }

        public static IConfiguration Configuration { get; private set; }

        // This method gets called by the runtime. Use this method to add services to the container
        public void ConfigureServices(IServiceCollection services)
        {
            // https://docs.microsoft.com/en-us/aspnet/core/fundamentals/http-requests?view=aspnetcore-3.1
            // In order to query Cognito's endpoint later in TestsController...
            services.AddHttpClient(); 

            // CORS Policy
            services.AddCors(options =>
            {                
                options.AddPolicy("AWSomeCORSPolicy", //Enable CORS with attributes
                    builder =>
                    {
                        builder.AllowAnyOrigin() // builder.WithOrigins("http://www.francas.team");
                                .AllowAnyMethod()
                                .AllowAnyHeader(); 
                    });
            });

            services.AddControllers();

            // Parameter Store
            services.Configure<RekognitionOptions>(
                _config.GetSection(RekognitionOptions.SectionName));         
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseCors(); // CORS with default policy and middleware OR Enable CORS with attributes

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapGet("/", async context =>
                {
                    await context.Response.WriteAsync("Welcome to running ASP.NET Core on AWS Lambda");
                });
            });
        }
    }
}
