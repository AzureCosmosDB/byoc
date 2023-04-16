using System;
using System.Configuration;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using Embeddings.Models;

namespace DataCopilot.Services
{
    public class Redis
    {
        // Redis Cache for Embeddings

        private static readonly string redisConnectionString = Environment.GetEnvironmentVariable("RedisConnection");

        public async Task CacheEmbeddings(Embedding emb, RedisConnection redisConnection, ILogger log)
        {
            try
            {
                // Perform cache operations using the cache object...
                log.LogInformation("Submitting data to cache");

                await Task.Run(() => RunRedisCommandsAsync(emb, redisConnection, log));
            }
            finally
            {
                //_redisConnection.Dispose();
            }
        }

        private static async Task RunRedisCommandsAsync(Embedding emb, RedisConnection redisConnection, ILogger log)
        {
            // Simple PING command
            // log.LogInformation($"{Environment.NewLine}: Cache command: PING");
            // RedisResult pingResult = await redisConnection.BasicRetryAsync(async (db) => await db.ExecuteAsync("PING"));
            // log.LogInformation($"Cache response: {pingResult}");

            bool stringSetResult = await redisConnection.BasicRetryAsync(async (db) => await db.StringSetAsync(emb.id, JsonSerializer.Serialize(emb)));
            log.LogInformation($"{Environment.NewLine}: Cache response from storing serialized Embedding object: {stringSetResult}");

            // Retrieve serialized object from cache
            RedisValue getMessageResult = await redisConnection.BasicRetryAsync(async (db) => await db.StringGetAsync(emb.id));
            Embedding embFromCache = JsonSerializer.Deserialize<Embedding>(getMessageResult);
            log.LogInformation($"Deserialized Embedding .NET object:{Environment.NewLine}");
            log.LogInformation($"Embedding.type : {embFromCache.type}");
            log.LogInformation($"Embedding.embeddings   : {embFromCache.embeddings.ToString()}");
            log.LogInformation($"Employee.id  : {embFromCache.id}{Environment.NewLine}");
        }

    }
}
