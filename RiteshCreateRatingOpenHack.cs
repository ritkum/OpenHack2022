using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Net.Http;
using Microsoft.Azure.Cosmos;
using System.Net;
using System.Collections.Generic;
using System.Linq;

namespace Company.Function
{
    public static class RiteshCreateRatingOpenHack
    {
        private static readonly string EndpointUri = "https://riteshopenhack.documents.azure.com:443/";
        private static readonly string PrimaryKey = "c0i0MRgkm5iDX7HwGKIUhg75iEJWDUtZpI25oIneW5zFzDIdEn0Er3AXE36zlQOnHUDDzxy8ZPGzYzzTPdCmkw==";
        private static CosmosClient cosmosClient;

        // The database we will create
        private static Database database;

        // The container we will create.
        private static Container container;

        // The name of the database and container we will create
        private static string databaseId = "Ratings";
        private static string containerId = "Rating";

        [FunctionName("CreateRating")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);

            string id = Convert.ToString(System.Guid.NewGuid());
            string userId = data.userId;
            string productId = data.productId;
            string locationName = data.locationName;
            string userNotes = data.userNotes;
            int rating = data.rating;
            string timestamp = System.DateTime.Now.ToString();

            if (rating >= 0 && rating <= 5)
            {
                bool validUser = await ValidateUser(userId);
                bool validProduct = await ValidateProduct(productId);

                if (validUser == true && validProduct == true)
                {
                    cosmosClient = new CosmosClient(EndpointUri, PrimaryKey);
                    database = await cosmosClient.CreateDatabaseIfNotExistsAsync(databaseId);
                    container = await database.CreateContainerIfNotExistsAsync(containerId, "/id");

                    Models.Ratings productRatings = new Models.Ratings
                    {
                        id = id,
                        userId = userId,
                        productId = productId,
                        locationName = locationName,
                        userNotes = userNotes,
                        rating = rating,
                        timeStamp = timestamp
                    };

                    await AddItemsToContainerAsync(productRatings);
                    return new OkObjectResult(productRatings);

                }
                else
                {
                    string errorMessage = "Invalid Product or user details";
                    return new BadRequestObjectResult(errorMessage);
                }

            }

            else
            {
                string errorMessage = "Ratings should be between 1 to 5";
                return new BadRequestObjectResult(errorMessage);
            }
        }

        public static async Task<bool> ValidateUser(string userid)
        {
            using (HttpClient client = new HttpClient())
            {

                var path = "https://serverlessohapi.azurewebsites.net/api/GetUser?userId=" + userid;
                HttpResponseMessage response = await client.GetAsync(path);
                if (response.IsSuccessStatusCode)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }

        }

        public static async Task<bool> ValidateProduct(string productid)
        {
            using (HttpClient client = new HttpClient())
            {

                var path = "https://serverlessohapi.azurewebsites.net/api/GetProduct?productId=" + productid;
                HttpResponseMessage response = await client.GetAsync(path);
                if (response.IsSuccessStatusCode)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }

        }

        private static async Task AddItemsToContainerAsync(Models.Ratings productDetails)
        {
            try
            {
                // Read the item to see if it exists.  
                ItemResponse<Models.Ratings> productDetailsValidate = await container.ReadItemAsync<Models.Ratings>(productDetails.id, new PartitionKey(productDetails.id));

            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                // Create an item in the container representing the Andersen family. Note we provide the value of the partition key for this item, which is "Andersen"
                ItemResponse<Models.Ratings> productDetailsSaveResponse = await container.CreateItemAsync<Models.Ratings>(productDetails, new PartitionKey(productDetails.id));
            }
        }
    
        [FunctionName("GetRating")]
        public static async Task<IActionResult> GetRating(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string ratingId = req.Query["ratingId"];

            if (ratingId is not null)
            {
                cosmosClient = new CosmosClient(EndpointUri, PrimaryKey);
                database = await cosmosClient.CreateDatabaseIfNotExistsAsync(databaseId);
                container = await database.CreateContainerIfNotExistsAsync(containerId, "/id");

                var sqlQueryText = string.Format("SELECT * FROM ratings WHERE ratings.id = '{0}'", ratingId);

                QueryDefinition queryDefinition = new QueryDefinition(sqlQueryText);
                FeedIterator<Models.Ratings> queryResultSetIterator = container.GetItemQueryIterator<Models.Ratings>(queryDefinition);

                List<Models.Ratings> rat = new List<Models.Ratings>();

                while (queryResultSetIterator.HasMoreResults)
                {
                    FeedResponse<Models.Ratings> currentResultSet = await queryResultSetIterator.ReadNextAsync();
                    foreach (Models.Ratings r in currentResultSet)
                    {
                        rat.Add(r);
                    }
                }
                return new OkObjectResult(rat.First());
            }
            else
            {
                string errorMessage = "Invalid Product details";
                return new BadRequestObjectResult(errorMessage);
            }

        }

        [FunctionName("GetRatings")]
        public static async Task<IActionResult> GetRatings(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            string userId = req.Query["userId"];

            if (userId is not null)
            {
                cosmosClient = new CosmosClient(EndpointUri, PrimaryKey);
                database = await cosmosClient.CreateDatabaseIfNotExistsAsync(databaseId);
                container = await database.CreateContainerIfNotExistsAsync(containerId, "/id");

                var sqlQueryText = string.Format("SELECT * FROM ratings WHERE ratings.userId = '{0}'", userId);

                QueryDefinition queryDefinition = new QueryDefinition(sqlQueryText);
                FeedIterator<Models.Ratings> queryResultSetIterator = container.GetItemQueryIterator<Models.Ratings>(queryDefinition);

                List<Models.Ratings> rat = new List<Models.Ratings>();

                while (queryResultSetIterator.HasMoreResults)
                {
                    FeedResponse<Models.Ratings> currentResultSet = await queryResultSetIterator.ReadNextAsync();
                    foreach (Models.Ratings r in currentResultSet)
                    {
                        rat.Add(r);
                    }
                }
                return new OkObjectResult(rat);
            }
            else
            {
                string errorMessage = "Invalid Product details";
                return new BadRequestObjectResult(errorMessage);
            }

        }

    }

}