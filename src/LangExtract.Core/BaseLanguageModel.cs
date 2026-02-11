using System.Text.Json;

namespace LangExtract.Core
{
    public abstract class BaseLanguageModel : ILanguageModel
    {
        public abstract Task<List<string>> InferAsync(string prompt, CancellationToken cancellationToken = default);

        public virtual async Task<List<List<string>>> InferBatchAsync(IEnumerable<string> prompts, CancellationToken cancellationToken = default)
        {
            var results = new List<List<string>>();
            foreach (var prompt in prompts)
            {
                results.Add(await InferAsync(prompt, cancellationToken));
            }
            return results;
        }

        public virtual object ParseOutput(string output)
        {
            try
            {
                // Simple JSON parsing wrapper
                // In a real scenario, we might want more robust handling or YAML support
                 return JsonSerializer.Deserialize<object>(output) ?? new object();
            }
            catch (Exception ex)
            {
                 throw new ArgumentException($"Failed to parse output: {ex.Message}", ex);
            }
        }
    }
}
