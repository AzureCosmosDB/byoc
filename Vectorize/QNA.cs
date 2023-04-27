using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Azure.AI.OpenAI;
using StackExchange.Redis;
using System.Net;
using Microsoft.AspNetCore.Mvc;
using Vectorize.Utils;
using Vectorize.Services;
using Vectorize.Models;

namespace Vectorize
{
    public class Chat
    {
        private readonly OpenAI _openAI = new OpenAI();
        private static Redis _redis;
        private static CosmosDB cosmosConnection = new CosmosDB(Environment.GetEnvironmentVariable("CosmosDBConnection"));

        [FunctionName("Chat")]
        public async Task<IActionResult> Run([HttpTrigger(
            AuthorizationLevel.Function, "get", "post", Route = null)]
            HttpRequest req,
            ILogger log)
        {
            string? _errorMessage;

            QueryModel query = new();
            //ChatRequest? chatRequest;
            ChatCompletionsOptions? chatRequest = null;
            ChatCompletions? chatResponse = null;

            int tokensUsed;

            if (req == null)
                return new NullResult();

            _redis = new Redis(log);
            // query = get_param(req, 'query')
            // session_id = get_param(req, 'session_id')
            // filter_param = get_param(req, 'filter')
            // search_method = get_param(req, 'search_method')

            string filter_param = req.Query["filter"];
            string session_id = req.Query["session_id"];
            query.QueryText = req.Query["prompt"];
            string search_method = req.Query["search_method"];

            log.LogInformation("Processing the query: " + query.QueryText);
            try
            {
                if (chatRequest is null || query.ResetContext)
                {
                    var resultList = new List<string>(query.ResultsToShow);
                    tokensUsed = 0;
                    query.ResetContext = false;
                    _errorMessage = "";

                    //Get Embeddings for user prompt
                    var vector = await _openAI.GetEmbeddingsAsync(query.QueryText, log);

                    var db = _redis.GetDatabase();
                    var memory = new ReadOnlyMemory<float>(vector);

                    //Search Redis for similar embeddings
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

                        string originalId = null, pk = null;
                        EmbeddingType embeddingType = EmbeddingType.unknown;

                        for (var i = 0; i < count; i++)
                        {
                            //fetch the RedisResult
                            RedisResult[] result = (RedisResult[])results[2 * i + 1 + 1];

                            if (result == null)
                                continue;

                            for (int j = 0; j < result.Length; j += 2)
                            {
                                var key = (string)result[j];
                                switch (key)
                                {
                                    case "pk":
                                        pk = (string)result[j + 1];
                                        break;

                                    case "originalId":
                                        originalId = (string)result[j + 1];
                                        break;

                                    case "type":
                                        embeddingType = (EmbeddingType)ushort.Parse((string)result[j + 1]);
                                        break;
                                }
                            }

                            var doc = await cosmosConnection.GetDocumentById(embeddingType, originalId, pk);

                            if (doc != null)
                                resultList.Add(doc);
                        }

                        chatRequest = _openAI.GetChatRequest(query.QueryText, resultList.Select(bd => bd), log);
                        chatResponse = await _openAI.GetChatResponse(chatRequest, log);
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
                    chatRequest.Messages.Add(new ChatMessage(ChatRole.User, query.QueryText));

                    chatResponse = await _openAI.GetChatResponse(chatRequest, log);
                    if (chatResponse?.Choices?[0]?.Message is { } m)
                    {
                        chatRequest.Messages.Add(m);
                        tokensUsed = chatResponse?.Usage?.PromptTokens ?? 0;
                        tokensUsed += chatResponse?.Usage?.CompletionTokens ?? 0;
                        query.QueryText = "";
                    }
                }

                var response = req.HttpContext.Response;

                response.StatusCode = (int)HttpStatusCode.OK;
                response.ContentType = "text/json; charset=utf-8";

                if (chatResponse != null)
                    await response.WriteAsync(chatResponse?.Choices?[0]?.Message.Content);

            }
            catch (Exception e)
            {
                _errorMessage = e.ToString();
                log.LogError(_errorMessage);
            }

            return new NullResult();
        }

    }
    public class NullResult : IActionResult
    {
        public Task ExecuteResultAsync(ActionContext context)
        => Task.FromResult(Task.CompletedTask);
    }
}
