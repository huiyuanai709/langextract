using LangExtract.Core;

namespace LangExtract.Logic.Prompting;

public class PromptTemplateStructured
{
    public string Description { get; set; }
    public List<ExampleData> Examples { get; set; }

    public PromptTemplateStructured(string description, List<ExampleData>? examples = null)
    {
        Description = description;
        Examples = examples ?? [];
    }
}