using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Vectorize.Models;
using Vectorize.Services;

namespace Vectorize
{
    public class CustomersAndOrders
    {

        private readonly OpenAI _openAI = new OpenAI();

        private static Redis _redis;


        [FunctionName("CustomersAndOrders")]
        public async Task Run(
            [CosmosDBTrigger(
                databaseName: "CosmicWorksDB",
                containerName: "customer",
                StartFromBeginning = true,
                Connection = "CosmosDBConnection",
                LeaseContainerName = "leases",
                CreateLeaseContainerIfNotExists = true)]IReadOnlyList<JObject> input,
            [CosmosDB(
                databaseName: "CosmicWorksDB",
                containerName: "embedding",
                Connection = "CosmosDBConnection")]IAsyncCollector<Embedding> output,
            ILogger log)
        {
            if (input != null && input.Count > 0)
            {
                log.LogInformation("Generating embeddings for " + input.Count + "Customers and Sales Orders");

                try
                {
                    foreach (dynamic item in input)
                    {

                        if (item.type == "customer")
                        {
                            Customer customer = item.ToObject<Customer>();
                            await GenerateCustomerEmbeddings(customer, output, log);

                        }
                        else
                        if (item.type == "salesOrder")
                        {
                            SalesOrder salesOrder = item.ToObject<SalesOrder>();
                            await GenerateOrderEmbeddings(salesOrder, output, log);

                        }

                    }
                }
                finally
                {

                }
            }
        }


        public async Task GenerateCustomerEmbeddings(Customer customer, IAsyncCollector<Embedding> output, ILogger log)
        {

            //Serialize the customer object to send to OpenAI
            string sCustomer = JObject.FromObject(customer).ToString(Newtonsoft.Json.Formatting.None);
            //int len = sCustomer.Length;


            Embedding embedding = new Embedding();
            embedding.id = Guid.NewGuid().ToString();
            embedding.type = EmbeddingType.customer;
            embedding.originalId = customer.id;

            try
            {
                //Get the embeddings from OpenAI
                var listEmbeddings = await _openAI.GetEmbeddingsAsync(sCustomer, log);

                //Add to embeddings object
                embedding.embeddings = listEmbeddings;
            }
            catch (Exception x)
            {
                log.LogError("Exception while generating embeddings for [" + customer.firstName + " " + customer.lastName + "]: " + x.Message);
            }


            //Insert embeddings into Cosmos DB
            await output.AddAsync(embedding);

            //Update Redis Cache with embeddings
            if (_redis == null)
                _redis = new Redis(log);

            await _redis.CacheEmbeddings(embedding, log);

            log.LogInformation("Cached embeddings for customer : " + customer.firstName + " " + customer.lastName);
        }

        public async Task GenerateOrderEmbeddings(SalesOrder salesOrder, IAsyncCollector<Embedding> output, ILogger log)
        {

            //Serialize the salesOrder to send to OpenAI
            string sSalesOrder = JObject.FromObject(salesOrder).ToString();
            //int len = sSalesOrder.Length;


            Embedding embedding = new Embedding();
            embedding.id = Guid.NewGuid().ToString();
            embedding.type = EmbeddingType.order;
            embedding.originalId = salesOrder.id;

            try
            {

                //Get the embeddings from OpenAI
                var listEmbeddings = await _openAI.GetEmbeddingsAsync(sSalesOrder, log);

                //Add to embeddings object
                embedding.embeddings = listEmbeddings;
            }
            catch (Exception x)
            {
                log.LogError("Exception while generating embeddings for [" + salesOrder.id + "]: " + x.Message);
            }


            //Insert embeddings into Cosmos DB
            await output.AddAsync(embedding);

            //Update Redis Cache with embeddings
            if (_redis == null)
                _redis = new Redis(log);

            await _redis.CacheEmbeddings(embedding, log);

            log.LogInformation("Cached embeddings for Sales Order Id: " + salesOrder.id);
        }
    }
}
