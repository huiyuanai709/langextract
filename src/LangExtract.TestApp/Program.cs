using LangExtract.Core;
using LangExtract.Core.Schema;
using LangExtract.Logic;
using LangExtract.Logic.Visualization;
using LangExtract.Providers;

namespace LangExtract.TestApp
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var config = ConfigManager.LoadConfig();

            // 1. Setup Mock/Stub Language Model
            var llmProvider = new OpenAIProvider(config.ApiKey!, config.Model, config.ApiEndpoint);

            // 2. Initialize Client
            var client = new LangExtractClient(
                languageModel: llmProvider,
                formatMode: FormatMode.Json
            );

            // 3. Define Data
            var examples = new List<ExampleData>
            {
                new ExampleData("John Doe lives in New York.", new List<Extraction>
                {
                    new Extraction("Person", "John Doe"),
                    new Extraction("Location", "New York")
                })
            };
            var promptDesc = "Extract list of entities from the text.";
            var textToProcess = "Apple Inc. announced a new iPhone today in California.";

            Console.WriteLine($"Processing text: {textToProcess}");

            try 
            {
                var result = await client.ExtractAsync(
                    textOrUrl: textToProcess,
                    promptDescription: promptDesc,
                    examples: examples
                );

                Console.WriteLine($"\nAnnotation Result (DocId: {result.DocumentId}):");
                foreach (var extraction in result.Extractions!)
                {
                    Console.WriteLine($"- [{extraction.ExtractionClass}] {extraction.ExtractionText}");
                    if (extraction.TokenInterval != null)
                        Console.WriteLine($"  Tokens: {extraction.TokenInterval.StartIndex}-{extraction.TokenInterval.EndIndex} ({extraction.AlignmentStatus})");
                }
                
                var html = Visualizer.Visualize(result);
                await File.WriteAllTextAsync("visualization_test.html", html);
                Console.WriteLine("Visualization generated at visualization_test.html");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during extraction: {ex}");
            }
        }
    }
}
