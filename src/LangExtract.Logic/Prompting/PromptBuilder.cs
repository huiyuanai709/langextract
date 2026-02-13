namespace LangExtract.Logic.Prompting;

public class PromptBuilder
{
    protected readonly QaPromptGenerator _generator;

    public PromptBuilder(QaPromptGenerator generator)
    {
        _generator = generator;
    }

    public virtual string BuildPrompt(string chunkText, string documentId, string? additionalContext = null)
    {
        return _generator.Render(chunkText, additionalContext);
    }
}