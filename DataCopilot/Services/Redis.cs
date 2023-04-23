using System;
using System.Configuration;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using DataCopilot.Models;
using DataCopilot.Utils;

namespace DataCopilot.Services
{
    public class Redis : IDisposable
    {
        // Redis Cache for Embeddings

        //private static readonly string redisConnectionString = Environment.GetEnvironmentVariable("RedisConnection");

        private static readonly ConnectionMultiplexer _connectionMultiplexer = ConnectionMultiplexer.Connect(Environment.GetEnvironmentVariable("RedisConnection"));
        ILogger log;

        // private static async Task RunRedisCommandsAsync(Embedding emb, ILogger log)
        // {
        //     // Simple PING command
        //     // log.LogInformation($"{Environment.NewLine}: Cache command: PING");
        //     // RedisResult pingResult = await redisConnection.BasicRetryAsync(async (db) => await db.ExecuteAsync("PING"));
        //     // log.LogInformation($"Cache response: {pingResult}");

        //     bool stringSetResult = await redisConnection.BasicRetryAsync(async (db) => await db.StringSetAsync(emb.id, JsonSerializer.Serialize(emb)));
        //     log.LogInformation($"{Environment.NewLine}: Cache response from storing serialized Embedding object: {stringSetResult}");

        //     // Retrieve serialized object from cache
        //     RedisValue getMessageResult = await redisConnection.BasicRetryAsync(async (db) => await db.StringGetAsync(emb.id));
        //     Embedding embFromCache = JsonSerializer.Deserialize<Embedding>(getMessageResult);
        //     log.LogInformation($"Deserialized Embedding .NET object:{Environment.NewLine}");
        //     log.LogInformation($"Embedding.type : {embFromCache.type}");
        //     log.LogInformation($"Embedding.embeddings   : {embFromCache.embeddings.ToString()}");
        //     log.LogInformation($"Embedding.id  : {embFromCache.id}{Environment.NewLine}");
        // }



        string? _errorMessage;
        List<string> _statusMessages = new();

        public Redis(ILogger log)
        {
            this.log = log;
            CreateRedisIndex();
        }

        public IDatabase GetDatabase()
        {
            return _connectionMultiplexer.GetDatabase();
        }

        void ClearState()
        {
            _errorMessage = "";
            _statusMessages.Clear();
        }

        public async Task CreateRedisIndex()
        {
            ClearState();

            try
            {
                log.LogInformation("Checking if Redis index exists...");
                var db = _connectionMultiplexer.GetDatabase();

                RedisResult index = null;
                try
                {
                    index = await db.ExecuteAsync("FT.INFO", "embeddingIndex");
                }
                catch (StackExchange.Redis.RedisServerException redisX)
                {
                    log.LogInformation("Exception while checking embedding index:" + redisX.Message);
                }
                if (index != null)
                {
                    log.LogInformation("Redis index for embeddings already exists. Skipping...");
                    return;
                }
                
                log.LogInformation("Creating Redis index...");
                //var db = _connectionMultiplexer.GetDatabase();
                var _ = await db.ExecuteAsync("FT.CREATE",
                    "embeddingIndex", "SCHEMA", "vector", "VECTOR", "HNSW", "6", "TYPE", "FLOAT32", "DISTANCE_METRIC", "COSINE", "DIM", "1536");
                log.LogInformation("Created Redis index for embeddings");
            }
            catch (Exception e)
            {
                _errorMessage = e.ToString();
                log.LogError(_errorMessage);
            }
        }

        public async Task CacheEmbeddings(Embedding emb, ILogger log)
        {
            try
            {
                // Perform cache operations using the cache object...
                log.LogInformation("Submitting embedding to cache");

                var db = _connectionMultiplexer.GetDatabase();

                var mem = new ReadOnlyMemory<float>(emb.embeddings);

                await db.HashSetAsync(emb.id, new[]{
                                                        new HashEntry("vector", mem.AsBytes()),
                                                        //new HashEntry("data", document.data)
                                                    });
                //return vector;
            }
            catch (Exception e)
            {
                _errorMessage = e.ToString();
            }
        }
        /*
        async Task RestoreRedisStateFromCosmosDB()
        {
            ClearState();

            try
            {
                _statusMessages.Add("Deleting all redis keys...");
                var db = _connectionMultiplexer.GetDatabase();
                var _ = await db.ExecuteAsync("FLUSHDB");
                _statusMessages.Add("Done.");

                _statusMessages.Add("Processing documents...");
                await foreach (var doc in cosmosConnection.GetAllDocuments())
                {
                    await BillDocument.CacheDocumentVector(openAIClient, db, doc);
                    _statusMessages.Add($"\tCached document with id '{doc.id}'");
                }
                _statusMessages.Add("Done.");
            }
            catch (Exception e)
            {
                _errorMessage = e.ToString();
            }
        }
*/
        public void Dispose()
        {
            _connectionMultiplexer.Dispose();
        }

    }
}
