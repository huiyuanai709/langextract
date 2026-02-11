using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LangExtract.Core;
using LangExtract.Core.Schema;
using LangExtract.Logic;

// using LangExtract; // Helper facade namespace

namespace LangExtract.TestApp
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("LangExtract C# Port Verification");

            // 1. Setup Mock/Stub Language Model
            var mockLLM = new MockLanguageModel();

            // 2. Initialize Client
            var client = new LangExtractClient(
                languageModel: mockLLM,
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
            string promptDesc = "Extract list of entities from the text.";
            string textToProcess = "Apple Inc. announced a new iPhone today in California.";

            Console.WriteLine($"Processing text: {textToProcess}");

            try 
            {
                var result = await client.ExtractAsync(
                    textOrUrl: textToProcess,
                    promptDescription: promptDesc,
                    examples: examples
                );

                Console.WriteLine($"\nAnnotation Result (DocId: {result.DocumentId}):");
                foreach (var extraction in result.Extractions)
                {
                    Console.WriteLine($"- [{extraction.ExtractionClass}] {extraction.ExtractionText}");
                    if (extraction.TokenInterval != null)
                        Console.WriteLine($"  Tokens: {extraction.TokenInterval.StartIndex}-{extraction.TokenInterval.EndIndex} ({extraction.AlignmentStatus})");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during extraction: {ex}");
            }
        }
    }

    // Simple Mock LLM for testing logic flow
    public class MockLanguageModel : BaseLanguageModel
    {
        public override Task<List<string>> InferAsync(string prompt, System.Threading.CancellationToken cancellationToken = default)
        {
             // Simulate output based on prompt
             // We can even check if prompt contains strict schema/examples
             
            string jsonOutput = @"
```json
{
  ""extractions"": [
    { ""extraction_class"": ""Organization"", ""extraction_text"": ""Apple Inc."" },
    { ""extraction_class"": ""Product"", ""extraction_text"": ""iPhone"" },
    { ""extraction_class"": ""Location"", ""extraction_text"": ""California"" }
  ]
}
```";
            return Task.FromResult(new List<string> { jsonOutput });
        }
    }
}
