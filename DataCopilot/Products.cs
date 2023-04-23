using DataCopilot.Models;
using DataCopilot.Services;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DataCopilot
{
    public class Products
    {

        private readonly OpenAI _openAI = new OpenAI();

        private static Redis _redis;

        [FunctionName("Products")]
        public async Task Run(
            [CosmosDBTrigger(
                databaseName: "CosmicWorksDB",
                containerName: "product",
                StartFromBeginning = true,
                Connection = "CosmosDBConnection",
                LeaseContainerName = "leases",
                CreateLeaseContainerIfNotExists = true)]IReadOnlyList<Product> input,
            [CosmosDB(
                databaseName: "CosmicWorksDB",
                containerName: "embedding",
                Connection = "CosmosDBConnection")]IAsyncCollector<Embedding> output,
            ILogger log)
        {
            if (input != null && input.Count > 0)
            {
                log.LogInformation("Generating embeddings for " + input.Count + " products");
                try
                {
                    foreach (Product item in input)
                    {
                        await GenerateProductEmbeddings(item, output, log);
                    }
                }
                finally
                {
                }
            }
        }

        public async Task GenerateProductEmbeddings(Product product, IAsyncCollector<Embedding> output, ILogger log)
        {
            //Serialize the product object to send to OpenAI
            string sProduct = JObject.FromObject(product).ToString();
            //int len = sProduct.Length;

            
            Embedding embedding = new Embedding();
            embedding.id = Guid.NewGuid().ToString();
            embedding.type = EmbeddingType.Product;

            try
            {

                //Get the embeddings from OpenAI
                var listEmbeddings = await _openAI.GetEmbeddingsAsync(sProduct, log);


                //Add to embeddings object
                embedding.embeddings = (float[])listEmbeddings;
            }
            catch (Exception x)
            {
                log.LogError("Exception while generating embeddings for [" + product.name + "]: " + x.Message);
            }


            //Insert embeddings into Cosmos DB
            await output.AddAsync(embedding);
            

            //Update Redis Cache with embeddings
            _redis = new Redis(log);
            await _redis.CacheEmbeddings(embedding, log);

            log.LogInformation("Cached embeddings for product: " + product.name);
        }
    }
}
