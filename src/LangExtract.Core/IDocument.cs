namespace LangExtract.Core;

/// <summary>
/// Document interface for annotating documents.
/// </summary>
public interface IDocument
{
    /// <summary>
    /// Raw text representation for the document.
    /// </summary>
    string Text { get; }

    /// <summary>
    /// Unique identifier for each document.
    /// </summary>
    string DocumentId { get; }

    /// <summary>
    /// Additional context to supplement prompt instructions.
    /// </summary>
    string? AdditionalContext { get; }
}