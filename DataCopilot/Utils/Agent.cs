using Azure;
using Azure.AI.OpenAI;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DataCopilot.Utils;

public class Agent
{
    private static readonly string openAIEndpoint = Environment.GetEnvironmentVariable("OpenAIEndpoint");
    private static readonly string openAIKey = Environment.GetEnvironmentVariable("OpenAIKey");
    private static readonly string openAIDeployment = Environment.GetEnvironmentVariable("OpenAIDeployment");
    private static readonly int openAIMaxTokens = int.Parse(Environment.GetEnvironmentVariable("OpenAIMaxTokens"));

    private readonly OpenAIClient client = new(new Uri(openAIEndpoint), new AzureKeyCredential(openAIKey));
}
