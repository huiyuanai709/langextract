using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LangExtract.Core;
using Microsoft.Extensions.AI;

namespace LangExtract.Providers
{
    public class OllamaProvider : OpenAIProvider
    {
        public OllamaProvider(string modelId = "llama3", string baseUrl = "http://localhost:11434/v1") 
            : base("ollama", modelId, baseUrl)
        {
        }
    }
}
