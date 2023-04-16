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
        private readonly Container _embeddingContainer = _cosmos.GetContainer("CosmicWorksDB", "embedding");

        private static RedisConnection _redisConnection;
        private static Redis _redis = new Redis();

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
                try
                {
                    _redisConnection = await RedisConnection.InitializeAsync(connectionString: Environment.GetEnvironmentVariable("RedisConnection").ToString());

                    foreach (Product item in input)
                    {
                        await GenerateProductEmbeddings(item, log);
                    }
                }
                finally
                {
                    _redisConnection.Dispose();
                }
            }
        }

        public async Task GenerateProductEmbeddings(Product product, ILogger log)
        {
            //Make a JSON string from the product object
            string foo = JObject.FromObject(product).ToString();
            int len = foo.Length;
            Embedding emb = new Embedding();
            emb.id = Guid.NewGuid().ToString();
            emb.type = EmbeddingType.Product;

            try
            {
                //Get the embeddings from OpenAI
                var bar = await _openAI.GetEmbeddingsAsync(foo, log);
                //Update Customer object with embeddings
                emb.embeddings = (List<float>)bar;
            }
            catch (Exception x)
            {
                log.LogError("Exception while generating embeddings for [" + product.name + "]: " + x.Message);
            }


            //Update Cosmos DB with embeddings
            await _embeddingContainer.CreateItemAsync(emb);

            //Update Redis Cache with embeddings
            await _redis.CacheEmbeddings(emb, _redisConnection, log);

            log.LogInformation("Generated embeddings for product: " + product.name);
        }
    }
}
