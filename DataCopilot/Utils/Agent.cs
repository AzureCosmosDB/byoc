using Azure;
using Azure.AI.OpenAI;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using StackExchange.Redis;
using DataCopilot.Services;

namespace DataCopilot.Utils;

public class Agent
{
    private static readonly string openAIEndpoint = Environment.GetEnvironmentVariable("OpenAIEndpoint");
    private static readonly string openAIKey = Environment.GetEnvironmentVariable("OpenAIKey");
    private static readonly string openAIDeployment = Environment.GetEnvironmentVariable("OpenAIDeployment");
    private static readonly int openAIMaxTokens = int.Parse(Environment.GetEnvironmentVariable("OpenAIMaxTokens"));

    private readonly OpenAIClient client = new(new Uri(openAIEndpoint), new AzureKeyCredential(openAIKey));

    public struct AgentResult
    {
        string answer;
        List<string> sources;
        string session_id;
    }

    //final_answer, sources, likely_sources, session_id = agent.run(query, session_id, redis_conn, filter_param)
    // public AgentResult Run(string query, RedisConnection redisConnection, ILogger log)
    // {
    //     try
    //     {
    //         // Do vector matching with Redis first
    //         log.LogInformation("Vector matching in Redis...");

    //         await Task.Run(() => RunRedisCommandsAsync(emb, redisConnection, log));

    //         // Get answer through Completions API

    //     }
    //     finally
    //     {
    //         //_redisConnection.Dispose();
    //     }
    // }
}
