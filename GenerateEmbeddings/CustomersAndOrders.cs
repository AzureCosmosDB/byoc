using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Embeddings.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Azure.Cosmos;
using GenerateEmbeddings.Services;

namespace Embeddings
{
    public class CustomersAndOrders
    {

        private readonly OpenAI _openAI = new OpenAI();

        private static readonly CosmosClient _cosmos = new CosmosClient(Environment.GetEnvironmentVariable("CosmosDBConnection"));
        //private readonly Container _customerContainer = _cosmos.GetContainer("CosmicWorksDB", "customer");
        private readonly Container _embeddingContainer = _cosmos.GetContainer("CosmicWorksDB", "embedding");

        [FunctionName("CustomersAndOrders")]
        public async Task Run([CosmosDBTrigger(
            databaseName: "CosmicWorksDB",
            containerName: "customer",
            StartFromBeginning = true,
            Connection = "CosmosDBConnection",
            LeaseContainerName = "leases",
            CreateLeaseContainerIfNotExists = true)]IReadOnlyList<JObject> input,
            ILogger log)
        {
            if (input != null && input.Count > 0)
            {
                log.LogInformation("Generating embeddings for " + input.Count + "Customers and Sales Orders");


                foreach (dynamic item in input)
                {

                    if (item.type == "customer")
                    {
                        Customer customer = item.ToObject<Customer>();
                        await GenerateCustomerEmbeddings(customer, log);

                    }
                    else
                    if (item.type == "salesOrder")
                    {
                        SalesOrder salesOrder = item.ToObject<SalesOrder>();
                        await GenerateOrderEmbeddings(salesOrder, log);

                    }

                }

            }
        }


        public async Task GenerateCustomerEmbeddings(Customer customer, ILogger log)
        {

            //Make a JSON string from the customer object
            string foo = JObject.FromObject(customer).ToString(Newtonsoft.Json.Formatting.None);
            int len = foo.Length;
            Embedding emb = new Embedding();
            emb.id = Guid.NewGuid().ToString();
            emb.type = EmbeddingType.Customer;

            try
            {
                //Get the embeddings from OpenAI
                var bar = await _openAI.GetEmbeddingsAsync(foo, log);
                //Update Customer object with embeddings
                emb.embeddings = (List<float>)bar;
            }
            catch (Exception x)
            {
                log.LogError("Exception while generating embeddings for [" + customer.firstName + " " + customer.lastName + "]: " + x.Message);
            }


            //Update Cosmos DB with embeddings
            await _embeddingContainer.CreateItemAsync(emb);

            //To-Do: Update Redis Cache with embeddings


            log.LogInformation("Generated Embeddings for customer : " + customer.firstName + " " + customer.lastName);
        }

        public async Task GenerateOrderEmbeddings(SalesOrder salesOrder, ILogger log)
        {

            //Make a JSON string from the salesOrder object
            string foo = JObject.FromObject(salesOrder).ToString();
            int len = foo.Length;
            Embedding emb = new Embedding();
            emb.id = Guid.NewGuid().ToString();
            emb.type = EmbeddingType.Order;

            try
            {
                //Get the embeddings from OpenAI
                var bar = await _openAI.GetEmbeddingsAsync(foo, log);
                //Update SalesOrder object with embeddings
                emb.embeddings = (List<float>)bar;
            }
            catch (Exception x)
            {
                log.LogError("Exception while generating embeddings for [" + salesOrder.id + "]: " + x.Message);
            }


            //Update Cosmos DB with embeddings
            await _embeddingContainer.CreateItemAsync(emb);


            //To-Do: Update Redis Cache with embeddings


            log.LogInformation("Generated Embeddings for Sales Order Id: " + salesOrder.id);
        }
    }
}
