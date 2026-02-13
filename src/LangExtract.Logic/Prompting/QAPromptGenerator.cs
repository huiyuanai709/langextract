using System.Text;
using LangExtract.Core;

namespace LangExtract.Logic.Prompting;

public class QaPromptGenerator
{
    public PromptTemplateStructured Template { get; set; }
    public FormatHandler FormatHandler { get; set; }
    public string ExamplesHeading { get; set; } = "Examples";
    public string QuestionPrefix { get; set; } = "Q: ";
    public string AnswerPrefix { get; set; } = "A: ";

    public QaPromptGenerator(PromptTemplateStructured template, FormatHandler? formatHandler = null)
    {
        Template = template;
        FormatHandler = formatHandler ?? new FormatHandler();
    }

    public string Render(string question, string? additionalContext = null)
    {
        var sb = new StringBuilder();
        sb.AppendLine(Template.Description);
        sb.AppendLine();

        if (!string.IsNullOrEmpty(additionalContext))
        {
            sb.AppendLine(additionalContext);
            sb.AppendLine();
        }

        if (Template.Examples.Count > 0)
        {
            sb.AppendLine(ExamplesHeading);
            foreach (var example in Template.Examples)
            {
                sb.AppendLine(FormatExampleAsText(example));
            }
            sb.AppendLine();
        }

        sb.AppendLine($"{QuestionPrefix}{question}");
        sb.Append(AnswerPrefix);

        return sb.ToString();
    }

    private string FormatExampleAsText(ExampleData example)
    {
        var answer = FormatHandler.FormatExtractionExample(example.Extractions);
        return $"{QuestionPrefix}{example.Text}\n{AnswerPrefix}{answer}\n";
    }
}