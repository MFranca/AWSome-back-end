using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

using Microsoft.Extensions.Logging; // builder.ConfigureLogging()
using Microsoft.Extensions.Configuration; // Parameter Store
using Amazon; // RegionEndpoint
using Amazon.Extensions.NETCore.Setup; // AWSOptions

namespace prjAWSomeBlog
{
    /// <summary>
    /// This class extends from APIGatewayProxyFunction which contains the method FunctionHandlerAsync which is the 
    /// actual Lambda function entry point. The Lambda handler field should be set to
    /// 
    /// prjAWSomeBlog::prjAWSomeBlog.LambdaEntryPoint::FunctionHandlerAsync
    /// </summary>
    public class LambdaEntryPoint : Amazon.Lambda.AspNetCoreServer.APIGatewayHttpApiV2ProxyFunction
    {
        /// <summary>
        /// The builder has configuration, logging and Amazon API Gateway already configured. The startup class
        /// needs to be configured in this method using the UseStartup<>() method.
        /// </summary>
        /// <param name="builder"></param>
        protected override void Init(IWebHostBuilder builder)
        {   
            builder.ConfigureAppConfiguration((hostingContext, config) => 
            {
                var env = hostingContext.HostingEnvironment;

                config.AddSystemsManager("/fra-awsomeblog", // Parameter store "prefix" - TODO: EXTERNAL PARAMETER
                new AWSOptions
                {
                    Region = RegionEndpoint.USEast2 // TODO: EXTERNAL PARAMETER
                });

                config
                    .SetBasePath(env.ContentRootPath)
                    .AddJsonFile("appsettings.json", optional: true)
                    .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true);

                config.AddEnvironmentVariables();
            });

            builder.ConfigureLogging((hostingContext, logging) =>
            {                
                logging.ClearProviders();
                logging.AddConfiguration(hostingContext.Configuration.GetSection("Logging"));
                logging.AddLambdaLogger(hostingContext.Configuration, "Logging");                
    
                // When you need logging below set the minimum level. Otherwise the logging framework will default to Informational for external providers.
                // logging.SetMinimumLevel(LogLevel.Debug);
            });

            builder.UseStartup<Startup>();
        }

        /// <summary>
        /// Use this override to customize the services registered with the IHostBuilder. 
        /// 
        /// It is recommended not to call ConfigureWebHostDefaults to configure the IWebHostBuilder inside this method.
        /// Instead customize the IWebHostBuilder in the Init(IWebHostBuilder) overload.
        /// </summary>
        /// <param name="builder"></param>
        protected override void Init(IHostBuilder builder)
        {
        }
    }
}
