using System;
using System.Configuration;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using Embeddings.Models;

namespace GenerateEmbeddings.Services
{
    public class Redis
    {
        // Redis Cache for Embeddings
        //private static RedisConnection _redisConnection;

        public async Task CacheEmbeddings(Embedding emb, RedisConnection redisConnection, ILogger log)
        {
            //_redisConnection = await RedisConnection.InitializeAsync(connectionString: ConfigurationManager.AppSettings["CacheConnection"].ToString());

            try
            {
                // Perform cache operations using the cache object...
                log.LogInformation("Submitting data to cache");

                // while (!Console.KeyAvailable)
                // {
                //     Task thread1 = Task.Run(() => RunRedisCommandsAsync("Thread 1"));
                //     Task thread2 = Task.Run(() => RunRedisCommandsAsync("Thread 2"));

                //     Task.WaitAll(thread1, thread2);
                // }

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
            log.LogInformation($"{Environment.NewLine}: Cache command: PING");
            RedisResult pingResult = await redisConnection.BasicRetryAsync(async (db) => await db.ExecuteAsync("PING"));
            log.LogInformation($"Cache response: {pingResult}");

            // // Simple get and put of integral data types into the cache
            // string key = "Message";
            // string value = "Hello! The cache is working from a .NET console app!";

            // log.LogInformation($"{Environment.NewLine}{prefix}: Cache command: GET {key} via StringGetAsync()");
            // RedisValue getMessageResult = await _redisConnection.BasicRetryAsync(async (db) => await db.StringGetAsync(key));
            // log.LogInformation($"{prefix}: Cache response: {getMessageResult}");

            // log.LogInformation($"{Environment.NewLine}{prefix}: Cache command: SET {key} \"{value}\" via StringSetAsync()");
            // bool stringSetResult = await _redisConnection.BasicRetryAsync(async (db) => await db.StringSetAsync(key, value));
            // log.LogInformation($"{prefix}: Cache response: {stringSetResult}");

            // log.LogInformation($"{Environment.NewLine}{prefix}: Cache command: GET {key} via StringGetAsync()");
            // getMessageResult = await _redisConnection.BasicRetryAsync(async (db) => await db.StringGetAsync(key));
            // log.LogInformation($"{prefix}: Cache response: {getMessageResult}");

            // Store serialized object to cache
            //Employee e007 = new Employee("007", "Davide Columbo", 100);
            //stringSetResult = await _redisConnection.BasicRetryAsync(async (db) => await db.StringSetAsync("e007", JsonSerializer.Serialize(e007)));
            bool stringSetResult = await redisConnection.BasicRetryAsync(async (db) => await db.StringSetAsync(emb.id, JsonSerializer.Serialize(emb)));
            log.LogInformation($"{Environment.NewLine}: Cache response from storing serialized Embedding object: {stringSetResult}");

            // Retrieve serialized object from cache
            RedisValue getMessageResult = await redisConnection.BasicRetryAsync(async (db) => await db.StringGetAsync(emb.id));
            Embedding embFromCache = JsonSerializer.Deserialize<Embedding>(getMessageResult);
            log.LogInformation($"Deserialized Embedding .NET object:{Environment.NewLine}");
            log.LogInformation($"Embedding.type : {embFromCache.type}");
            log.LogInformation($"Embedding.embeddings   : {embFromCache.embeddings}");
            log.LogInformation($"Employee.id  : {embFromCache.id}{Environment.NewLine}");
        }

    }
}
