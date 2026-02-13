namespace LangExtract.Core;

/// <summary>
/// Document class for annotating documents.
/// </summary>
public class Document : IDocument
{
    private string? _documentId;

    /// <summary>
    /// Gets or sets the raw text representation for the document.
    /// </summary>
    public string Text { get; set; }

    /// <summary>
    /// Gets or sets the additional context to supplement prompt instructions.
    /// </summary>
    public string? AdditionalContext { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Document"/> class.
    /// </summary>
    /// <param name="text">The raw text.</param>
    /// <param name="documentId">Optional document ID.</param>
    /// <param name="additionalContext">Optional additional context.</param>
    public Document(string text, string? documentId = null, string? additionalContext = null)
    {
        Text = text ?? throw new ArgumentNullException(nameof(text));
        _documentId = documentId;
        AdditionalContext = additionalContext;
    }

    /// <summary>
    /// Gets or sets the unique identifier for each document. Auto-generated if not set.
    /// </summary>
    public string DocumentId
    {
        get
        {
            if (_documentId == null)
            {
                _documentId = $"doc_{Guid.NewGuid().ToString("N").Substring(0, 8)}";
            }
            return _documentId;
        }
        set => _documentId = value;
    }

    /// <summary>
    /// Gets or sets the tokenized text for the document.
    /// </summary>
    public TokenizedText? TokenizedText { get; set; }
}