using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using LangExtract.Core;

namespace LangExtract.Logic.IO
{
    public static class JsonLHelper
    {
        public static async Task SaveAnnotatedDocumentsAsync(IEnumerable<AnnotatedDocument> documents, string outputPath)
        {
            using (var writer = new StreamWriter(outputPath))
            {
                foreach (var doc in documents)
                {
                    string line = JsonSerializer.Serialize(doc);
                    await writer.WriteLineAsync(line);
                }
            }
        }

        public static async IAsyncEnumerable<AnnotatedDocument> LoadAnnotatedDocumentsAsync(string inputPath)
        {
            using (var reader = new StreamReader(inputPath))
            {
                string? line;
                while ((line = await reader.ReadLineAsync()) != null)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    var doc = JsonSerializer.Deserialize<AnnotatedDocument>(line);
                    if (doc != null) yield return doc;
                }
            }
        }
    }
}
