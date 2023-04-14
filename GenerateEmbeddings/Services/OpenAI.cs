using Azure;
using Azure.AI.OpenAI;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GenerateEmbeddings.Services;

public class OpenAI
{

    private static readonly string openAIEndpoint = Environment.GetEnvironmentVariable("OpenAIEndpoint");
    private static readonly string openAIKey = Environment.GetEnvironmentVariable("OpenAIKey");
    private static readonly string openAIDeployment = Environment.GetEnvironmentVariable("OpenAIDeployment");
    private static readonly int openAIMaxTokens = int.Parse(Environment.GetEnvironmentVariable("OpenAIMaxTokens"));

    private readonly OpenAIClient client = new(new Uri(openAIEndpoint), new AzureKeyCredential(openAIKey));

    public async Task<IReadOnlyList<float>> GetEmbeddingsAsync(dynamic data, ILogger log)
    {

        try
        {

            EmbeddingsOptions options = new EmbeddingsOptions(data)
            {
                Input = data
                //InputType = "foo"
            };

            var response = await client.GetEmbeddingsAsync(openAIDeployment, options);

            Azure.AI.OpenAI.Embeddings embeddings = response.Value;

            IReadOnlyList<float> embedding = embeddings.Data[0].Embedding;

            return embedding;
        }
        catch (Exception ex)
        {
            log.LogError(ex.Message);
            return null;

        }
    }
}
