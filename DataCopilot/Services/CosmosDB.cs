using Microsoft.Azure.Cosmos;
using System.Text.Json;
using DataCopilot.Models;

namespace DataCopilot.Services;

public class CosmosDB
{
    private readonly CosmosClient _cosmosClient;

    public CosmosDB(string configuration)
    {
        _cosmosClient = new CosmosClient(configuration);
    }

    public async Task<int> CountRecords(string collectionName)
    {
        var container = _cosmosClient.GetContainer("CosmicWorksDB", collectionName);
        var feedIterator = container.GetItemQueryStreamIterator(new QueryDefinition("SELECT COUNT(1) FROM c"));
        var response = await feedIterator.ReadNextAsync();
        using var jsonDoc = JsonDocument.Parse(response.Content);
        return jsonDoc.RootElement.GetProperty("Documents")[0].GetProperty("$1").GetInt32();
    }

    public async Task<string> GetDocumentById(EmbeddingType embeddingType, string id, string partitionKey)
    {
        var container =_cosmosClient.GetContainer("CosmicWorksDB", (embeddingType == EmbeddingType.product) ? "product" : "customer");
        var response = await container.ReadItemStreamAsync(id, new PartitionKey(partitionKey));
        if ((int)response.StatusCode < 200 || (int)response.StatusCode >= 400)
        {
            throw new InvalidOperationException($"Failed to retrieve an item for id '{id}' - status code '{response.StatusCode}");
        }

        string content = null;
        
        using (StreamReader sr = new StreamReader(response.Content))
        content = await sr.ReadToEndAsync();

        return content;
    }

    public async IAsyncEnumerable<DocModel> GetAllDocuments(string collectionName)
    {
        var container = _cosmosClient.GetContainer("CosmicWorksDB", collectionName);
        using var feedIterator = container.GetItemQueryIterator<DocModel>("SELECT * FROM c");
        while (feedIterator.HasMoreResults)
        {
            var response = await feedIterator.ReadNextAsync();
            foreach (var item in response)
            {
                yield return item;
            }
        }
    }    

    public async Task<string> GetDocumentString(EmbeddingType embeddingType, string query)
    {
        if (embeddingType == null || query == null)
            return null;
        var container = _cosmosClient.GetContainer("CosmicWorksDB", (embeddingType == EmbeddingType.product) ? "product" : "customer");
         // Read existing item from container
        using FeedIterator feedIterator = container.GetItemQueryStreamIterator("SELECT * FROM c " + query);

        string content = null;
        if (feedIterator != null && feedIterator.HasMoreResults)
            using (ResponseMessage response = await feedIterator.ReadNextAsync())
                    {
                        using (StreamReader sr = new StreamReader(response.Content))
                        content = await sr.ReadToEndAsync();
                    }

        return content;
    }
}