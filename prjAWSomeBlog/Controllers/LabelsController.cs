using System;
using System.Collections.Generic;
//using System.Linq;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Cors;
using Microsoft.Extensions.Logging;

using Amazon;

using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;

namespace prjS3Upload.Controllers
{
    [Route("api/[controller]")]
    [Produces("application/Json")]
    public class LabelsController : ControllerBase
    {
        private readonly ILogger<LabelsController> _logger;
        private readonly AmazonDynamoDBClient _dbClient;
        private const string DYNAMODB_TABLE = "AWSomeRekognitionTB";

        public LabelsController(ILogger<LabelsController> logger)
        {
            this._logger = logger;

            AmazonDynamoDBConfig clientConfig = new AmazonDynamoDBConfig();
            clientConfig.RegionEndpoint = RegionEndpoint.USEast2; // TODO: External Configuration
            this._dbClient = new AmazonDynamoDBClient(clientConfig);
        }

        // GET api/labels
        [EnableCors("AWSomeCORSPolicy")]
        [HttpGet]
        public IActionResult GetAllDistinctLabels()
        {
            this._logger.LogInformation("*** public IActionResult GetAllDistinctLabels() ***");
            
            var labelsList = getLabels();
            labelsList.Sort();
            this._logger.LogInformation("labelsList count: " + labelsList.Count);             

            return Ok
            (
              labelsList
            );
        }

        // GET api/labels/{label}
        [EnableCors("AWSomeCORSPolicy")]
        [HttpGet("{label}")]
        public IActionResult GetVideosByLabel(string label) // maybe move this to VideosController?!
        {
            this._logger.LogInformation("*** public IActionResult GetVideosByLabel('{0}') ***", label);

            var videoList = getVideosByLabels(label);
            videoList.Sort();

            return Ok
            (
              videoList
            );
        }

        private List<string> getVideosByLabels(string label)
        {
            this._logger.LogInformation("*** private List<string> getVideosByLabels('{0}') ***", label);

            var newVideo = "";
            var videos = new List<string>();

            try
            {
                // Let us search the database for all videos with a specific tag/label...
                Table table = Table.LoadTable(this._dbClient, DYNAMODB_TABLE);                
                QueryFilter queryFilter = new QueryFilter("PK", QueryOperator.Equal, "LABEL#"+label);
                Search search = table.Query(queryFilter);                             
                List<Document> documentSet = new List<Document>();

                do
                {
                    documentSet = search.GetNextSetAsync().Result;
                    foreach (var document in documentSet)
                    {
                        var value = document["S3ObjectKey"];
                        if (value != newVideo)
                        {
                            newVideo = value;
                            videos.Add(value);
                            this._logger.LogInformation("We have found a video: " + value);
                        }
                    }
                } while (!search.IsDone);
            }
            catch (AmazonDynamoDBException e)
            {                
                this._logger.LogError("Amazon DynamoDB Error encountered on server. Message: " + e.Message);
            }
            catch (Exception e)
            {
                this._logger.LogError("Unknown Error encountered on server. Message: " + e.Message);
            }

            return videos;
        }

        private List<string> getLabels()
        {
            this._logger.LogInformation("*** private List<string> getLabels() ***");

            var newLabel = "";
            var labels = new List<string>();

            try
            {
                Table table = Table.LoadTable(this._dbClient, DYNAMODB_TABLE);

                // Remember: you can only use an equals operator on your partition key!
                // Let us get all available tags/labels...
                ScanFilter scanFilter = new ScanFilter();
                scanFilter.AddCondition("PK", ScanOperator.GreaterThanOrEqual, "LABEL");
                scanFilter.AddCondition("PK", ScanOperator.LessThan, "VIDEO");
                Search search = table.Scan(scanFilter); // watch out for scan operations!!!

                List<Document> documentSet = new List<Document>();
                do
                {
                    documentSet = search.GetNextSetAsync().Result;
                    foreach (var document in documentSet)
                    {
                        var value = document["Label"];
                        if (value != newLabel)
                        {
                            newLabel = value;
                            labels.Add(value);                            
                        }
                    }
                } while (!search.IsDone);
            }
            catch (AmazonDynamoDBException e)
            {                
                this._logger.LogError("Amazon DynamoDB Error encountered on server. Message: " + e.Message);
            }
            catch (Exception e)
            {
                this._logger.LogError("Unknown Error encountered on server. Message: " + e.Message);
            }

            return labels;
        }
    }
}