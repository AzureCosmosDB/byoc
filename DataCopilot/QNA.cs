using DataCopilot.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Configuration;
using System.Text.Json;
using System.Threading.Tasks;
using Azure.AI.OpenAI;
using StackExchange.Redis;
using DataCopilot.Utils;
using DataCopilot.Models;

namespace DataCopilot
{
    public class Chat
    {
        private readonly OpenAI _openAI = new OpenAI();
        private static Redis _redis = new Redis();
        private static CosmosConnection cosmosConnection = new CosmosConnection(Environment.GetEnvironmentVariable("CosmosDBConnection"));

        [FunctionName("Chat")]
        public async Task Run([HttpTrigger(
            AuthorizationLevel.Function, "get", "post", Route = null)]
            HttpRequest req,
            ILogger log)
        {
            string? _errorMessage;

            //IQueryable<BillDocument> queryResults = Array.Empty<BillDocument>().AsQueryable();
            
            QueryModel query = new();
            //ChatRequest? chatRequest;
            ChatCompletionsOptions? chatRequest = null;

            int tokensUsed;

            if (req == null)
                return;

            // query = get_param(req, 'query')
            // session_id = get_param(req, 'session_id')
            // filter_param = get_param(req, 'filter')
            // search_method = get_param(req, 'search_method')

            string filter_param = req.Query["filter"];
            string session_id =  req.Query["session_id"];
            string prompt =  req.Query["prompt"];                
            string search_method =  req.Query["search_method"];
            
            log.LogInformation("Processing the query: " + prompt);
            try
            {

                if (chatRequest is null || query.ResetContext)
                {
                    var resultList = new List<DocModel>(query.ResultsToShow);
                    tokensUsed = 0;
                    query.ResetContext = false;
                    _errorMessage = "";

                    var vector = await _openAI.GetEmbeddingsAsync(query.QueryText, log);
                    var db = _redis.GetDatabase();
                    var memory = new ReadOnlyMemory<float>(vector);

                    var res = await db.ExecuteAsync("FT.SEARCH",
                        "embeddingIndex",
                        $"*=>[KNN {query.ResultsToShow} @vector $BLOB]",
                        "PARAMS",
                        "2",
                        "BLOB",
                        memory.AsBytes(),
                        "SORTBY",
                        "__vector_score",
                        "DIALECT",
                        "2");

                    if (res.Type == ResultType.MultiBulk)
                    {
                        var results = (RedisResult[])res;
                        var count = (int)results[0];
                        if ((2 * count + 1) != results.Length)
                        {
                            throw new NotSupportedException($"Unexpected entries is Redis result, '{results.Length}' results for count of '{count}'");
                        }

                        for (var i = 0; i < count; i++)
                        {
                            var id = (string)results[2 * i + 1];

                            var doc = await cosmosConnection.GetDocumentById(id);

                            resultList.Add(doc);
                        }

                        //queryResults = resultList.AsQueryable();

                        chatRequest = _openAI.GetChatRequest(query.QueryText, resultList.Select(bd => bd.ToString()), log); //TODO: return actual document payload
                        var chatResponse = await _openAI.GetChatResponse(chatRequest, log);
                        if (chatResponse?.Choices?[0]?.Message is { } m)
                        {
                            chatRequest.Messages.Add(m);
                            tokensUsed += chatResponse?.Usage?.PromptTokens ?? 0;
                            tokensUsed += chatResponse?.Usage?.CompletionTokens ?? 0;
                        }
                        query.QueryText = "";
                    }
                    else
                    {
                        throw new NotSupportedException($"Unexpected query result type {res.Type}");
                    }
                }
                else
                {
                    chatRequest.Messages.Add(new ChatMessage (ChatRole.User, query.QueryText));

                    var chatResponse = await _openAI.GetChatResponse(chatRequest, log);
                    if (chatResponse?.Choices?[0]?.Message is { } m)
                    {
                        chatRequest.Messages.Add(m);
                        tokensUsed = chatResponse?.Usage?.PromptTokens ?? 0;
                        tokensUsed += chatResponse?.Usage?.CompletionTokens ?? 0;
                        query.QueryText = "";
                    }
                }
            }
            catch (Exception e)
            {
                _errorMessage = e.ToString();
                log.LogError(_errorMessage);
            }
            finally
            {
            }
        }
    }
}
