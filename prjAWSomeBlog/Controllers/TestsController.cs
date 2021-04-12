using System.Net.Http;
using System.Linq;
using System;
using System.Collections.Generic;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Cors;

using Amazon.Lambda.Core; // LambdaLogger
using Amazon.Lambda.AspNetCoreServer;
using Amazon.Lambda.APIGatewayEvents;

using System.Text.Json; // JsonSerializer

using prjS3Upload.Models; // UserInfo

namespace prjS3Upload.Controllers
{
    // Public APIs for testing purpose...
    [Route("api/[controller]")]
    [Produces("application/json")] // important!
    public class TestsController : ControllerBase // without view support (MVC), since it is not a SSR
    {
        private readonly ILogger<TestsController> _logger;
        private readonly IHttpClientFactory _clientFactory;

        public TestsController
        (ILogger<TestsController> logger, 
        IHttpClientFactory clientFactory)
        {
            this._logger = logger;
            this._clientFactory = clientFactory;
        }

        // GET api/tests/version
        [HttpGet("version")]
        public IActionResult Version() // https://docs.microsoft.com/en-us/aspnet/core/web-api/action-return-types?view=aspnetcore-3.1
        {
            this._logger.LogInformation("*** public IActionResult Version() ***");

            return Ok
            (
                new { version = "1.0.77" }
            );
        }

        // PUT api/tests/about
        [EnableCors("AWSomeCORSPolicy")] // https://docs.microsoft.com/en-us/aspnet/core/security/cors?view=aspnetcore-3.1#attr
        [HttpPut("about")]
        public IActionResult About() 
        {
            this._logger.LogInformation("*** public ContentResult About() ***");
            
            return Ok
            (
                new
                {
                    message = "AWSome blog SERVED VIA PUT - just for testing purposes...",
                    other = "bla, bla, bla",
                    cors = "[EnableCors(AWSomeCORSPolicy)]"
                }
            );
        }

        // GET api/tests
        [HttpGet]
        public IEnumerable<string> Get()
        {
            this._logger.LogInformation("*** public IEnumerable<string> Get() ***");
            return new string[] { "one", "two", "three" };
        }

        // GET api/tests/{id}
        [HttpGet("{id}")]
        public string Get(string id)
        {            
            this._logger.LogInformation("*** public string Get(string id) ***");
            this._logger.LogInformation("Id: " + id);
            return "Hello World, my blog post: " + id;
        }

        // PUT api/tests/{id}
        [EnableCors("AWSomeCORSPolicy")]
        [HttpPut("{id}")]        
        public IActionResult Put([FromRoute] String id, [FromBody] string rawBody) 
        {
            //https://aws.amazon.com/blogs/developer/updates-for-net-core-lambda-libraries/
            // if locally (no apigateway), it will return NULL!!!
            var lambdaContext = (ILambdaContext)Request.HttpContext.Items[APIGatewayHttpApiV2ProxyFunction.LAMBDA_CONTEXT];
            var lambdaRequestObject = (APIGatewayHttpApiV2ProxyRequest)Request.HttpContext.Items[APIGatewayHttpApiV2ProxyFunction.LAMBDA_REQUEST_OBJECT];
            var authorizer = lambdaRequestObject.RequestContext.Authorizer;

            string token = lambdaRequestObject.Headers["authorization"].ToString();
            string region = Environment.GetEnvironmentVariable("AWS_REGION"); //!!!
            string userInfoUrl = Environment.GetEnvironmentVariable("userinfoEndpoint");
            
            this._logger.LogInformation("User Info URL: " + userInfoUrl);                                    
            this._logger.LogInformation("Tenant id: " + getTenantId(userInfoUrl, token));

            string lambdaClaims = "";
            if (lambdaRequestObject != null)
            {
                if (lambdaRequestObject.RequestContext.Authorizer != null)
                {   
                    // AWS_ACCESS_KEY_ID, AWS_SECRET_ACCESS_KEY, AWS_SESSION_TOKEN – The access keys obtained from the function's execution role.                  
                    var accessId = Environment.GetEnvironmentVariable("AWS_ACCESS_KEY_ID");
                    this._logger.LogInformation("Cognito Groups: " + authorizer.Jwt.Claims["cognito:groups"]);

                    if (authorizer.Jwt.Claims.ContainsKey("cognito:roles"))
                        this._logger.LogInformation("Cognito Roles: " + authorizer.Jwt.Claims["cognito:roles"]);
                    else
                        this._logger.LogInformation("No Cognito Roles");

                    this._logger.LogInformation("Memory Limit: " + lambdaContext.MemoryLimitInMB);
                    this._logger.LogInformation("Region: " + region);
                    this._logger.LogInformation("Access Key: " + accessId);
                    
                    if (lambdaRequestObject.RequestContext.Authorizer.Jwt != null)
                    {
                        foreach (string key in lambdaRequestObject.RequestContext.Authorizer.Jwt.Claims.Keys)
                        {
                            lambdaClaims += " *Key*: " + key + " - value: " + lambdaRequestObject.RequestContext.Authorizer.Jwt.Claims[key].ToString();
                        }                        
                    }
                    else
                        lambdaClaims = "JWT is null!";
                }
                else
                    lambdaClaims = "Authorizer is null!";
            }

            object resultado = null;
            if (lambdaContext != null)
            {
                resultado = new
                {
                    versao = "20.12.16",
                    nome = "parameter: " + id,
                    body = rawBody, // need to be sent between "
                    cognito_region = region,

                    funcion_name = lambdaContext.FunctionName,
                    function_version = lambdaContext.FunctionVersion,
                    log_group_name = lambdaContext.LogGroupName,
                    identity_pool_id = lambdaContext.Identity.IdentityPoolId,
                    identity_id = lambdaContext.Identity.IdentityId,
                    remaining_time = lambdaContext.RemainingTime,

                    FirstOrDefaultKey = lambdaRequestObject.PathParameters.Keys.FirstOrDefault(),
                    lambdaRequestObjectRequestContextAuthorizerJwtClaimsKeys = lambdaClaims
                };
            }
            else
            {
                resultado = new
                {
                    versao = "0.0.3",
                    nome = "parameter: " + id,
                    body = rawBody, // need to be sent between "
                    description = "probally called locally"
                };
            }

            return Ok(resultado); // HTTP 200
        }

        private string getTenantId(string userInfoUrl, string token) 
        {
            // To query Cognito endpoint for requesting additional information about the authenticated user...
            string tenantId = "";

            var request = new HttpRequestMessage(HttpMethod.Get, userInfoUrl);            
            request.Headers.Add("Accept", "*/*");
            request.Headers.Add("User-Agent", "LambdaAgent");
            request.Headers.Add("authorization", token);

            // using IHttpClientFactory, which can be registered by calling AddHttpClient in Startup.cs
            var client = _clientFactory.CreateClient();
            var response = client.SendAsync(request).Result;

            if (response.IsSuccessStatusCode)
            {
                this._logger.LogInformation("success!");
                this._logger.LogInformation("Response StatusCode: " + response.StatusCode.ToString());
                
                string responseBody = response.Content.ReadAsStringAsync().Result.Replace("custom:", "");
                var userInfo = JsonSerializer.Deserialize<UserInfo>(responseBody);
                                
                this._logger.LogInformation("username: " + userInfo.username);
                this._logger.LogInformation("email: " + userInfo.email);
                this._logger.LogInformation("role: " + userInfo.userRole);
                this._logger.LogInformation("tenant: " + userInfo.tenantId);

                tenantId = userInfo.tenantId;
            } 
            else
            {
                this._logger.LogInformation("Failure! It was not possible to request addifional user information from Cognito...");
            }

            return tenantId;
        }
    }
}