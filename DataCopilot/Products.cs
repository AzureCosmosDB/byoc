using Embeddings.Models;
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

        private static RedisConnection _redisConnection;
        private static Redis _redis = new Redis();

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
                    _redisConnection = await RedisConnection.InitializeAsync(connectionString: Environment.GetEnvironmentVariable("RedisConnection").ToString());

                    foreach (Product item in input)
                    {
                        await GenerateProductEmbeddings(item, output, log);
                    }
                }
                finally
                {
                    _redisConnection.Dispose();
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
                embedding.embeddings = (List<float>)listEmbeddings;
            }
            catch (Exception x)
            {
                log.LogError("Exception while generating embeddings for [" + product.name + "]: " + x.Message);
            }


            //Insert embeddings into Cosmos DB
            await output.AddAsync(embedding);
            

            //Update Redis Cache with embeddings
            await _redis.CacheEmbeddings(embedding, _redisConnection, log);

            log.LogInformation("Generated embeddings for product: " + product.name);
        }
    }
}
