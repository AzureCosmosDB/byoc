using Azure;
using Azure.AI.OpenAI;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DataCopilot.Services;

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

    private const string SystemPromptStart =
       """
       Assistant is an intelligent chatbot designed to help users answer their document-related questions.
       Instructions:
       - Only answer questions related to the documents provided below.
       - If you're unsure of an answer, you can say "I don't know" or "I'm not sure" and recommend users search themselves.

       Text of relevant documents:
       """;

    public async Task<string> GetChatResponse(string request, IEnumerable<string> context, ILogger log)
    {
        try
        {
            log.LogInformation($"Input: {request}");
            ChatCompletionsOptions chatOptions = new ChatCompletionsOptions();

            var content = new StringBuilder(SystemPromptStart);
            foreach (var c in context)
            {
                content.Append("- ").AppendLine(c);
            }

            chatOptions.Messages.Add(new ChatMessage("system", content.ToString()));
            chatOptions.Messages.Add(new ChatMessage("user", request));           

            Response<ChatCompletions> completionsResponse = await client.GetChatCompletionsAsync(openAIDeployment, chatOptions);
            string completion = completionsResponse.Value.Choices[0].Message.Content;
            log.LogInformation($"Output: {completion}");

            return completion;
        }
        catch (Exception ex)
        {
            log.LogError(ex.Message);
            return null;
        }
    }
}

public class EmbeddingRequest
{
    public string input { get; set; } = "";
}

public class ChatResponse
{
    public Choice[] choices { get; set; } = Array.Empty<Choice>();
    public Usage? usage { get; set; }

    public string Content => choices[0].message?.content ?? "";
}

public class Choice
{
    public Message? message { get; set; }
}

public class Message
{
    public string content { get; set; } = "";
    public string role { get; set; } = "";
}

public class Usage
{
    public int completion_tokens { get; set; }
    public int prompt_tokens { get; set; }
    public int total_tokens { get; set; }
}
