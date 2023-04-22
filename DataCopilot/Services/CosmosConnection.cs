using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using System.Reflection.Metadata;
using System.Text.Json;
using DataCopilot.Models;

namespace DataCopilot.Services;

public class CosmosConnection
{
    private readonly CosmosClient _cosmosClient;

    public CosmosConnection(string configuration)
    {
        _cosmosClient = new CosmosClient(configuration);
    }

    public async Task<int> CountRecords()
    {
        var container = _cosmosClient.GetContainer("databaseName", "collectionName");
        var feedIterator = container.GetItemQueryStreamIterator(new QueryDefinition("SELECT COUNT(1) FROM c"));
        var response = await feedIterator.ReadNextAsync();
        using var jsonDoc = JsonDocument.Parse(response.Content);
        return jsonDoc.RootElement.GetProperty("Documents")[0].GetProperty("$1").GetInt32();
    }

    public async Task<DocModel> GetDocumentById(string id)
    {
        var container = _cosmosClient.GetContainer("databaseName", "collectionName");
        var response = await container.ReadItemAsync<DocModel>(id, new PartitionKey(id));
        if ((int)response.StatusCode < 200 || (int)response.StatusCode >= 400)
        {
            throw new InvalidOperationException($"Failed to retrieve an item for id '{id}' - status code '{response.StatusCode}");
        }

        return response.Resource;
    }

    public async IAsyncEnumerable<DocModel> GetAllDocuments()
    {
        var container = _cosmosClient.GetContainer("databaseName", "collectionName");
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

    public async Task WriteItem(DocModel doc)
    {
        var container = _cosmosClient.GetContainer("databaseName", "collectionName");
        var response = await container.CreateItemAsync(doc, new PartitionKey(doc.id));
        if ((int)response.StatusCode < 200 || (int)response.StatusCode >= 400)
        {
            throw new InvalidOperationException($"Failed to retrieve an item for id '{doc.id}' - status code '{response.StatusCode}");
        }
    }
}
