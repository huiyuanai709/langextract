using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using LangExtract.Logic;
using LangExtract.Core;
using LangExtract.Core.Schema;
using Microsoft.SemanticKernel;

namespace LangExtract.Agents
{
    public class ExtractionPlugin
    {
        private readonly LangExtractClient _client;

        public ExtractionPlugin(LangExtractClient client)
        {
            _client = client;
        }

        [KernelFunction("extract_information")]
        [Description("Extracts structured information from text based on a description.")]
        [return: Description("The extracted information in JSON format.")]
        public async Task<string> ExtractAsync(
            [Description("The text to extract information from")] string text, 
            [Description("Description of what to extract")] string promptDescription,
            CancellationToken cancellationToken = default)
        {
            // For the plugin usage, we might not have examples passed dynamically yet.
            // We use an empty list for now.
            var examples = new System.Collections.Generic.List<ExampleData>();
            
            var result = await _client.ExtractAsync(
                textOrUrl: text, 
                promptDescription: promptDescription, 
                examples: examples,
                cancellationToken: cancellationToken);
            
            return new FormatHandler().FormatExtractionExample(result.Extractions); 
        }
    }
}
