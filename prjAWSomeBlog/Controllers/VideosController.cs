using System;
using System.Net.Http;
using System.Text.Json;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Cors;
using Microsoft.Extensions.Logging;

using Amazon;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.AspNetCoreServer;

using Amazon.S3;
using Amazon.S3.Model;

using prjS3Upload.Models;

namespace prjS3Upload.Controllers
{
    [Route("api/[controller]")]
    [Produces("application/Json")]
    public class VideosController : ControllerBase 
    {
        private readonly ILogger<VideosController> _logger;
        private readonly IHttpClientFactory _clientFactory;
        private readonly IOptions<RekognitionOptions> _rekognitionOptions;        
        
        public VideosController(
            IOptions<RekognitionOptions> rekognitionOptions, 
            ILogger<VideosController> logger, 
            IHttpClientFactory clientFactory) 
        {
            this._rekognitionOptions = rekognitionOptions;
            this._logger = logger;
            this._clientFactory = clientFactory;
        }
        
        private const Double TIMEOUT_DURATION = 6; // in hours
        private static readonly RegionEndpoint bucketRegion = RegionEndpoint.USEast2; // ohio TODO: CHANGE TO EXTERNAL CONFIGURATION FILE
        private static IAmazonS3 s3Client;

        // PUT api/videos/filename
        [EnableCors("AWSomeCORSPolicy")]
        [HttpPut("{id}")]        
        public IActionResult Put([FromRoute]String id) //JsonResult
        {
            this._logger.LogInformation("*** public JsonResult Put([FromRoute]String id) ***");
            
            var response = new 
            {
                Versao = "0.1.1",
                Nome = "nome do arquivo: " + id,
                URL = GenerateUploadPreSignedUrl(id), 
                bucketname = this._rekognitionOptions.Value.UploadBucketName // from Parameter Store
            };

            this._logger.LogInformation("Pre-Signed URL (put): " + response.URL);

            return Ok
            (
              response
            );
        }

        // GET api/videos/filename
        [EnableCors("AWSomeCORSPolicy")]
        [HttpGet("{video}")]        
        public IActionResult GetVideoByName([FromRoute]String video)
        {
            this._logger.LogInformation("*** public IActionResult GetVideoByName('{0}') ***", video);            

            var response = new 
            {                
                filename = video,
                bucketname = this._rekognitionOptions.Value.UploadBucketName, // from Parameter Store
                URL = GenerateDownloadPreSignedUrl(video) 
            };

            this._logger.LogInformation("Pre-Signed URL (get): " + response.URL); 

            return Ok
            (
              response
            );
        }
        
        private string GenerateUploadPreSignedUrl(String objectKey) 
        {
            string bucketName = this._rekognitionOptions.Value.UploadBucketName; // from Parameter Store
            this._logger.LogInformation("*** GenerateUploadPreSignedUrl *** Bucket name for upload: '" + bucketName);

            LogUserInfo(); // for debugging purpose only

            s3Client = new AmazonS3Client(bucketRegion);
            string urlString = "";

            try 
            {
                GetPreSignedUrlRequest request = new GetPreSignedUrlRequest {
                    BucketName = bucketName,
                    Key = objectKey,
                    Verb = HttpVerb.PUT,
                    Expires = DateTime.UtcNow.AddHours(TIMEOUT_DURATION),
                    ContentType = "video/mp4" // TODO CONVERT TO CONSTANT, and watch out for signaturedoesnotmatch-error
                };
                
                urlString = s3Client.GetPreSignedURL(request);
            }
            catch (AmazonS3Exception e) 
            {
                this._logger.LogError("Amazon S3 Error encountered on server. Message: " + e.Message);
                urlString = String.Format ("Amazon S3 Error encountered on server. Message: '{0}' when getting a pre-signed URL." + e.Message);
            }
            catch (Exception e) 
            {
                this._logger.LogError("Unknown Error encountered on server. Message: " + e.Message);
                urlString = String.Format ("Unknown Error encountered on server. Message: '{0}' when getting a pre-signed URL.", e.Message);                            
            }

            return urlString;
        }

        private string GenerateDownloadPreSignedUrl(String objectKey) 
        {
            string bucketName = this._rekognitionOptions.Value.UploadBucketName;
            this._logger.LogInformation("private string GenerateDownloadPreSignedUrl('{0}')", objectKey);
            this._logger.LogInformation("Bucket: '{0}'.", bucketName);

            LogUserInfo(); // for debugging purpose only

            s3Client = new AmazonS3Client(bucketRegion);
            string urlString = "";

            try 
            {
                GetPreSignedUrlRequest request = new GetPreSignedUrlRequest {
                    BucketName = bucketName,
                    Key = objectKey,                    
                    Expires = DateTime.UtcNow.AddHours(TIMEOUT_DURATION)                    
                }; 

                urlString = s3Client.GetPreSignedURL(request);
            }
            catch (AmazonS3Exception e) 
            {
                this._logger.LogError("Amazon S3 Error encountered on server. Message: " + e.Message);
                urlString = String.Format("Amazon S3 Error encountered on server. Message: '{0}' when getting a pre-signed URL." + e.Message);
            }
            catch (Exception e) 
            {
                this._logger.LogError("Unknown Error encountered on server. Message: " + e.Message);
                urlString = String.Format ("Unknown Error encountered on server. Message: '{0}' when getting a pre-signed URL.", e.Message);
            }

            return urlString;
        }

        private void LogUserInfo() 
        {   
            // Get additional info from the authenticated user by calling Cognito's endpoint...

            var lambdaRequestObject = 
                (APIGatewayHttpApiV2ProxyRequest)Request.HttpContext.Items[APIGatewayHttpApiV2ProxyFunction.LAMBDA_REQUEST_OBJECT];
            string token = lambdaRequestObject.Headers["authorization"].ToString();
            string region = Environment.GetEnvironmentVariable("AWS_REGION"); 
            string userInfoUrl = Environment.GetEnvironmentVariable("userinfoEndpoint");
            
            var request = new HttpRequestMessage(HttpMethod.Get, userInfoUrl);
            request.Headers.Add("Accept", "*/*");
            request.Headers.Add("User-Agent", "LambdaAgent");
            request.Headers.Add("authorization", token);

            var client = _clientFactory.CreateClient();
            var response = client.SendAsync(request).Result;

            if (response.IsSuccessStatusCode)
            {
                string responseBody = response.Content.ReadAsStringAsync().Result.Replace("custom:", ""); // remove "custom:"
                var userInfo = JsonSerializer.Deserialize<UserInfo>(responseBody);
                                
                this._logger.LogInformation("username: " + userInfo.username);
                this._logger.LogInformation("email: " + userInfo.email);
                this._logger.LogInformation("role: " + userInfo.userRole);
                this._logger.LogInformation("tenant: " + userInfo.tenantId);                
            }
            else
            {
                this._logger.LogError("Failure when checking UserInfo!");    
            }            
        }        
    }
}