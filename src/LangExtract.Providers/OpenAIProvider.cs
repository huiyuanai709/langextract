using LangExtract.Core;
using Microsoft.Extensions.AI;
using OpenAI;

namespace LangExtract.Providers;

public class OpenAIProvider : BaseLanguageModel
{
    private readonly IChatClient _chatClient;

    public OpenAIProvider(IChatClient chatClient)
    {
        _chatClient = chatClient;
    }

    public OpenAIProvider(string apiKey, string modelId = "gpt-4o-mini", string? baseUrl = null)
    {
        var options = new OpenAIClientOptions();
        if (!string.IsNullOrEmpty(baseUrl))
        {
            options.Endpoint = new Uri(baseUrl);
        }
            
        // If baseUrl is provided but no apiKey, OpenAI client might complain.
        // For local models (Ollama/LM Studio), apiKey can often be anything.
        if (string.IsNullOrEmpty(apiKey)) apiKey = "dummy-key";

        var openAiClient = new OpenAIClient(new System.ClientModel.ApiKeyCredential(apiKey), options);
        _chatClient = openAiClient.GetChatClient(modelId).AsIChatClient();
    }

    public override async Task<List<string>> InferAsync(string prompt, CancellationToken cancellationToken = default)
    {
        var response = await _chatClient.GetResponseAsync(prompt, cancellationToken: cancellationToken);
        return [response.Text];
    }
}