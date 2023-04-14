using Embeddings.Models;
using GenerateEmbeddings.Services;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GenerateEmbeddings
{
    public class Products
    {

        private readonly OpenAI _openAI = new OpenAI();

        private static readonly CosmosClient _cosmos = new CosmosClient(Environment.GetEnvironmentVariable("CosmosDBConnection"));
        private readonly Container _productContainer = _cosmos.GetContainer("CosmicWorksDB", "product");


        [FunctionName("Products")]
        public async Task Run([CosmosDBTrigger(
            databaseName: "CosmicWorksDB",
            containerName: "product",
            StartFromBeginning = true,
            Connection = "CosmosDBConnection",
            LeaseContainerName = "leases",
            CreateLeaseContainerIfNotExists = true)]IReadOnlyList<Product> input,
            ILogger log)
        {
            if (input != null && input.Count > 0)
            {
                log.LogInformation("Generating embeddings for " + input.Count + " products");


                foreach (Product item in input)
                {

                    await GenerateProductEmbeddings(item, log);

                }

            }
        }

        public async Task GenerateProductEmbeddings(Product product, ILogger log)
        {

            //Make a JSON string from the product object
            string foo = JObject.FromObject(product).ToString();


            //Get the embeddings from OpenAI
            var bar = await _openAI.GetEmbeddingsAsync(foo, log);

            //Update Customer object with embeddings
            product.embeddings = (List<float>)bar;

            //Update Cosmos DB with embeddings
            await _productContainer.ReplaceItemAsync(product, product.id, new PartitionKey(product.categoryId));

            //To-Do: Update Redis Cache with embeddings


            log.LogInformation("Generated embeddings for product: " + product.name);
        }
    }
}
