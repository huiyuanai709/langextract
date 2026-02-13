using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;

namespace LangExtract.Agents;

/// <summary>
/// An Agent specialized in extraction using LangExtract logic.
/// It wraps a ChatCompletionAgent configured with the ExtractionPlugin.
/// </summary>
public class ExtractionAgent
{
    public ChatCompletionAgent Agent { get; }

    public ExtractionAgent(string name, Kernel kernel, Logic.LangExtractClient client)
    {
        // Register the plugin in the kernel (or agent's kernel)
        kernel.Plugins.AddFromObject(new ExtractionPlugin(client), "Extraction");

        // Create the agent
        Agent = new ChatCompletionAgent
        {
            Name = name,
            Instructions = "You are an extraction specialist. Use the ExtractionPlugin to extract information from text.",
            Kernel = kernel
        };
    }
}