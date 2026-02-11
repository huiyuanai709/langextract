using LangExtract.Core;

namespace LangExtract.Providers
{
    public static class ProviderFactory
    {
        public static BaseLanguageModel CreateOpenAI(string apiKey, string modelId = "gpt-4o-mini", string? baseUrl = null)
        {
            return new OpenAIProvider(apiKey, modelId, baseUrl);
        }

        public static BaseLanguageModel CreateOllama(string modelId = "llama3", string baseUrl = "http://localhost:11434/v1")
        {
            return new OllamaProvider(modelId, baseUrl);
        }

        public static BaseLanguageModel CreateGemini(string apiKey, string modelId = "gemini-2.5-flash", HttpClient? httpClient = null)
        {
            return new GeminiProvider(apiKey, modelId, httpClient);
        }

        public static BaseLanguageModel CreateFromEnvironment()
        {
            // Simple logic to pick provider based on environment variables
            var geminiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY");
            if (!string.IsNullOrEmpty(geminiKey))
            {
                return CreateGemini(geminiKey);
            }

            var openAiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
            if (!string.IsNullOrEmpty(openAiKey))
            {
                return CreateOpenAI(openAiKey);
            }

            // Default to Ollama if no keys? Or throw?
            // Let's assume user wants to try Ollama if nothing else is configured
            return CreateOllama();
        }
    }
}
