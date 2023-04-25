﻿using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using DataCopilot.Models;
using DataCopilot.Utils;

namespace DataCopilot.Services
{
    // Redis Cache for Embeddings
    public class Redis : IDisposable
    {
        private static readonly ConnectionMultiplexer _connectionMultiplexer = ConnectionMultiplexer.Connect(Environment.GetEnvironmentVariable("RedisConnection"));
        private static CosmosDB _cosmosDB = new CosmosDB(Environment.GetEnvironmentVariable("CosmosDBConnection"));

        ILogger log;
        string? _errorMessage;
        List<string> _statusMessages = new();

        public Redis(ILogger log)
        {
            this.log = log;
            //CreateRedisIndex();  // TODO: uncomment in regular flow
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
                    //not returning - index most likely doesn't exist
                }
                if (index != null)
                {
                    log.LogInformation("Redis index for embeddings already exists. Skipping...");
                    return;
                }
                
                log.LogInformation("Creating Redis index...");

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
                                                        new HashEntry("originalId", emb.originalId),
                                                        new HashEntry("type", (int) emb.type),
                                                        new HashEntry("pk", emb.partitionKey)
                                                        //new HashEntry("data", document.data)
                                                    });
                //return vector;
            }
            catch (Exception e)
            {
                _errorMessage = e.ToString();
            }
        }
        
        async Task RestoreRedisStateFromCosmosDB(ILogger log)
        {
            ClearState();

            try
            {
                log.LogInformation("Deleting all Redis keys...");
                var db = _connectionMultiplexer.GetDatabase();
                var _ = await db.ExecuteAsync("FLUSHDB");
                log.LogInformation("Deleted all Redis keys.");

                // Populate cache with Cosmos DB collections' data
                //log.LogInformation("Repopulating cache...");
                // await foreach (var doc in _cosmosDB.GetAllDocuments()))
                // {
                //     await BillDocument.CacheDocumentVector(openAIClient, db, doc);
                //     _statusMessages.Add($"\tCached document with id '{doc.id}'");
                // }
                //log.LogInformation("Repopulated cache.");
            }
            catch (Exception e)
            {
                _errorMessage = e.ToString();
            }
        }

        public void Dispose()
        {
            _connectionMultiplexer.Dispose();
        }

    }
}
