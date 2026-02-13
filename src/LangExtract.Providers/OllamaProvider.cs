namespace LangExtract.Providers;

public class OllamaProvider : OpenAIProvider
{
    public OllamaProvider(string modelId = "llama3", string baseUrl = "http://localhost:11434/v1") 
        : base("ollama", modelId, baseUrl)
    {
    }
}