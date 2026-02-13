using LangExtract.Core;
using LangExtract.Core.Exceptions;
using LangExtract.Logic.Tokenizers;

namespace LangExtract.Logic.Resolvers;

public class Resolver
{
    private readonly FormatHandler _formatHandler;

    public Resolver(FormatHandler? formatHandler = null)
    {
        _formatHandler = formatHandler ?? new FormatHandler();
    }

    public List<Extraction> Resolve(string llmOutput, bool suppressParseErrors = false)
    {
        try 
        {
            var extractionData = _formatHandler.ParseOutputToList(llmOutput);
            return ExtractOrderedExtractions(extractionData);
        }
        catch (Exception ex) when (ex is FormatError || ex is ResolverParsingError)
        {
            if (suppressParseErrors)
            {
                Console.WriteLine($"Parse Error: {ex.Message}");
                return [];
            }
            throw new ResolverParsingError(ex.Message, ex);
        }
        catch (Exception ex)
        {
            if (suppressParseErrors)
            {
                Console.WriteLine($"Unexpected Error: {ex.Message}");
                return [];
            }
            throw;
        }
    }

    private List<Extraction> ExtractOrderedExtractions(List<Dictionary<string, object>> extractionData)
    {
        var extractions = new List<Extraction>();
        int extractionIndex = 0;
        string indexSuffix = "_index"; // Default hardcoded for now, pass in ctor if needed
        string attributeSuffix = _formatHandler.AttributeSuffix;

        for (int groupIndex = 0; groupIndex < extractionData.Count; groupIndex++)
        {
            var group = extractionData[groupIndex];
            foreach (var kvp in group)
            {
                string key = kvp.Key;
                object value = kvp.Value;

                // Skip attribute keys or index keys
                if (key.EndsWith(indexSuffix)) continue;
                if (key.EndsWith(attributeSuffix)) continue;

                // Basic type check
                if (!(value is string || value is int || value is float || value is double || value is long))
                {
                    // value usually converts to specific types in FormatHandler
                    continue; 
                }
                    
                string text = value.ToString() ?? "";
                    
                // Logic to get attributes if present
                Dictionary<string, object>? attributes = null;
                string attrKey = key + attributeSuffix;
                if (group.TryGetValue(attrKey, out var attrObj) && attrObj is Dictionary<string, object> attrDict)
                {
                    attributes = attrDict;
                }

                // Logic to get index if present
                // string idxKey = key + indexSuffix; ...

                extractions.Add(new Extraction(
                    extractionClass: key,
                    extractionText: text,
                    extractionIndex: extractionIndex++, 
                    groupIndex: groupIndex,
                    attributes: attributes
                ));
            }
        }

        return extractions;
    }

    public IEnumerable<Extraction> Align(
        IEnumerable<Extraction> extractions, 
        string sourceText, 
        int tokenOffset, 
        int charOffset,
        ITokenizer? tokenizer = null)
    {
        var aligner = new WordAligner();
        return aligner.AlignExtractions(extractions, sourceText, tokenOffset, charOffset, tokenizer);
    }
}