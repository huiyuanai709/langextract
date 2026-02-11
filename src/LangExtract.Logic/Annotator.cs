using System.Runtime.CompilerServices;
using LangExtract.Core;
using LangExtract.Core.Schema;
using LangExtract.Logic.Chunking;
using LangExtract.Logic.Prompting;
using LangExtract.Logic.Tokenizers;

namespace LangExtract.Logic
{
    public class Annotator
    {
        private readonly BaseLanguageModel _languageModel;
        private readonly QAPromptGenerator _promptGenerator;
        private readonly FormatHandler _formatHandler;

        public Annotator(
            BaseLanguageModel languageModel,
            PromptTemplateStructured promptTemplate,
            FormatMode formatType = FormatMode.Json)
        {
            _languageModel = languageModel;
            _formatHandler = new FormatHandler(formatType: formatType);
            _promptGenerator = new QAPromptGenerator(promptTemplate, _formatHandler);
        }

        public async Task<AnnotatedDocument> AnnotateTextAsync(
            string text,
            int maxCharBuffer = 200,
            string? additionalContext = null,
            int? contextWindowChars = null,
            ITokenizer? tokenizer = null,
            CancellationToken cancellationToken = default)
        {
            var document = new Document(text, additionalContext: additionalContext);
            var documents = new List<Document> { document };

            var results = new List<AnnotatedDocument>();

            await foreach (var annotatedDoc in AnnotateDocumentsAsync(
                               documents,
                               maxCharBuffer: maxCharBuffer,
                               contextWindowChars: contextWindowChars,
                               tokenizer: tokenizer,
                               cancellationToken: cancellationToken))
            {
                results.Add(annotatedDoc);
            }

            if (results.Count == 0) throw new Exception("No annotation results produced.");
            return results[0];
        }

        public async IAsyncEnumerable<AnnotatedDocument> AnnotateDocumentsAsync(
            IEnumerable<Document> documents,
            int maxCharBuffer = 200,
            int batchLength = 1,
            int? contextWindowChars = null,
            ITokenizer? tokenizer = null,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            tokenizer ??= new RegexTokenizer();
            var resolver = new Resolver(_formatHandler);

            // Simplified logic: process one by one or in batches
            // Here assuming simple iteration for clarity of port

            // We need to iterate chunks from all documents
            // The chunk iterator in python handles multiple docs

            var chunkIterator = new ChunkIteratorHelper(documents, maxCharBuffer, tokenizer);
            var chunks = chunkIterator.Iterate();

            var promptBuilder = new ContextAwarePromptBuilder(_promptGenerator, contextWindowChars);

            // Group chunks into batches (simplified: handle one by one for now or simple batching)
            var currentBatch = new List<TextChunk>();

            var extractionsByDocId = new Dictionary<string, List<Extraction>>();
            var textByDocId = new Dictionary<string, string>();

            foreach (var doc in documents)
            {
                textByDocId[doc.DocumentId] = doc.Text ?? "";
                extractionsByDocId[doc.DocumentId] = new List<Extraction>();
            }

            foreach (var chunk in chunks)
            {
                currentBatch.Add(chunk);
                if (currentBatch.Count >= batchLength)
                {
                    await ProcessBatchAsync(currentBatch, promptBuilder, resolver, extractionsByDocId, tokenizer,
                        cancellationToken);
                    currentBatch.Clear();
                }
            }

            if (currentBatch.Count > 0)
            {
                await ProcessBatchAsync(currentBatch, promptBuilder, resolver, extractionsByDocId, tokenizer,
                    cancellationToken);
            }

            foreach (var docId in extractionsByDocId.Keys)
            {
                yield return new AnnotatedDocument(
                    text: textByDocId[docId],
                    extractions: extractionsByDocId[docId],
                    documentId: docId
                );
            }
        }

        private async Task ProcessBatchAsync(
            List<TextChunk> batch,
            ContextAwarePromptBuilder promptBuilder,
            Resolver resolver,
            Dictionary<string, List<Extraction>> extractionsByDocId,
            ITokenizer tokenizer,
            CancellationToken cancellationToken)
        {
            var prompts = new List<string>();
            foreach (var chunk in batch)
            {
                prompts.Add(promptBuilder.BuildPrompt(chunk.ChunkText, chunk.Document.DocumentId,
                    chunk.Document.AdditionalContext));
            }

            // Infer
            // Assuming InferBatchAsync takes List<string> and returns List<List<string>> (candidates)
            // But we actually just need the first candidate mostly.
            // BaseLanguageModel.InferBatchAsync returns List<List<string>>

            var batchResults = await _languageModel.InferBatchAsync(prompts, cancellationToken);

            for (int i = 0; i < batch.Count; i++)
            {
                var chunk = batch[i];
                var results = batchResults[i];
                if (results.Count == 0) continue;

                string llmOutput = results[0];
                var resolved = resolver.Resolve(llmOutput);

                // Align
                int tokenOffset = chunk.TokenInterval.StartIndex;

                // Need char offset. TextChunk doesn't store it directly, but we can compute it from token
                // Actually TextChunk stores TokenInterval and Document.
                // We need to look up token at StartIndex in Document to get CharInterval.

                int charOffset = 0;
                if (chunk.Document?.TokenizedText != null &&
                    chunk.TokenInterval.StartIndex < chunk.Document.TokenizedText.Tokens.Count)
                {
                    charOffset = chunk.Document.TokenizedText.Tokens[chunk.TokenInterval.StartIndex].CharInterval
                        .StartPos ?? 0;
                }

                var aligned = resolver.Align(resolved, chunk.ChunkText, tokenOffset, charOffset, tokenizer);

                if (chunk.Document?.DocumentId != null)
                {
                    if (!extractionsByDocId.ContainsKey(chunk.Document.DocumentId))
                    {
                        extractionsByDocId[chunk.Document.DocumentId] = new List<Extraction>();
                    }

                    extractionsByDocId[chunk.Document.DocumentId].AddRange(aligned);
                }
            }
        }
    }

    // Helper for iterating chunks across multiple documents
    public class ChunkIteratorHelper
    {
        private readonly IEnumerable<Document> _documents;
        private readonly int _maxCharBuffer;
        private readonly ITokenizer _tokenizer;

        public ChunkIteratorHelper(IEnumerable<Document> documents, int maxCharBuffer, ITokenizer tokenizer)
        {
            _documents = documents;
            _maxCharBuffer = maxCharBuffer;
            _tokenizer = tokenizer;
        }

        public IEnumerable<TextChunk> Iterate()
        {
            foreach (var doc in _documents)
            {
                var iter = new ChunkIterator(doc, _maxCharBuffer, _tokenizer);
                foreach (var chunk in iter.Iterate())
                {
                    yield return chunk;
                }
            }
        }
    }
}