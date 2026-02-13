namespace LangExtract.Core;

/// <summary>
/// A single training/example data instance for structured prompting.
/// </summary>
public class ExampleData
{
    public string Text { get; set; }
    public List<Extraction> Extractions { get; set; }

    public ExampleData(string text, List<Extraction>? extractions = null)
    {
        Text = text;
        Extractions = extractions ?? new List<Extraction>();
    }
}