using Embeddings.Models;
using DataCopilot.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DataCopilot
{
    public class Chat
    {
        private readonly OpenAI _openAI = new OpenAI();
        private static RedisConnection _redisConnection;
        private static Redis _redis = new Redis();

        [FunctionName("Chat")]
        public async Task Run([HttpTrigger(
            AuthorizationLevel.Function, "get", "post", Route = null)]
            HttpRequest req,
            ILogger log)
        {
            if (req != null)
            {
                // query = get_param(req, 'query')
                // session_id = get_param(req, 'session_id')
                // filter_param = get_param(req, 'filter')
                // search_method = get_param(req, 'search_method')

                string filter_param = req.Query["filter"];
                string session_id =  req.Query["session_id"];
                string query =  req.Query["query"];                
                string search_method =  req.Query["search_method"];
                
                log.LogInformation("Generating embeddings for query: " + query);
                try
                {
                    _redisConnection = await RedisConnection.InitializeAsync(connectionString: Environment.GetEnvironmentVariable("RedisConnection").ToString());
                    // TODO: Generate embeddings for the question and match them with ones in Redis
                    //final_answer, sources, likely_sources, session_id = agent.run(query, session_id, redis_conn, filter_param)

                    await _openAI.GetChatResponse(query, new List<string>().AsQueryable(), log);
                }
                finally
                {
                    _redisConnection.Dispose();
                }
            }
        }
    }
}
