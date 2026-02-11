using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace LangExtract.Core
{
    public interface ILanguageModel
    {
        Task<List<string>> InferAsync(string prompt, CancellationToken cancellationToken = default);
        Task<List<List<string>>> InferBatchAsync(IEnumerable<string> prompts, CancellationToken cancellationToken = default);
        object ParseOutput(string output); // Returns Dictionary, List, or primitive
    }
}
