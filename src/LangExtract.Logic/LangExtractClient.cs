using LangExtract.Core;
using LangExtract.Core.Schema;
using LangExtract.Logic.IO;
using LangExtract.Logic.Prompting;
using LangExtract.Logic.Tokenizers;

namespace LangExtract.Logic
{
    /// <summary>
    /// Main client for using LangExtract functionality.
    /// Acts as a high-level facade over Annotator, Resolver, and other components.
    /// </summary>
    public class LangExtractClient
    {
        private readonly BaseLanguageModel _languageModel;
        private readonly FormatMode _formatMode;
        private readonly ITokenizer _tokenizer;

        public LangExtractClient(
            BaseLanguageModel languageModel,
            FormatMode formatMode = FormatMode.Json,
            ITokenizer? tokenizer = null)
        {
            _languageModel = languageModel;
            _formatMode = formatMode;
            _tokenizer = tokenizer ?? new RegexTokenizer();
        }

        public async Task<AnnotatedDocument> ExtractAsync(
            string textOrUrl,
            string promptDescription,
            List<ExampleData> examples,
            int maxCharBuffer = 1000,
            int batchLength = 10,
            int? contextWindowChars = null,
            string? additionalContext = null,
            CancellationToken cancellationToken = default)
        {
            // 1. Handle URL if needed
            string text = textOrUrl;
            if (Uri.TryCreate(textOrUrl, UriKind.Absolute, out var uriResult) 
                && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps))
            {
                 // Simple check, real app might need more robust URL validation logic from io.py
                 // But assuming UrlDownloader handles it.
                 try 
                 {
                     var downloader = new UrlDownloader();
                     text = await downloader.DownloadTextFromUrlAsync(textOrUrl);
                 }
                 catch (Exception ex)
                 {
                     // Fallback: maybe it wasn't a URL but just looked like one, or download failed.
                     // In the python version verify logic is stricter.
                     Console.WriteLine($"Warning: Failed to download URL, treating as text. Error: {ex.Message}");
                 }
            }

            // 2. Setup Components
            var promptTemplate = new PromptTemplateStructured(
                description: promptDescription,
                examples: examples
            );

            var annotator = new Annotator(
                languageModel: _languageModel,
                promptTemplate: promptTemplate,
                formatType: _formatMode
            );

            // 3. Run Annotation
            return await annotator.AnnotateTextAsync(
                text: text,
                maxCharBuffer: maxCharBuffer,
                additionalContext: additionalContext,
                contextWindowChars: contextWindowChars,
                tokenizer: _tokenizer,
                cancellationToken: cancellationToken
            );
        }
    }
}
