using System.Text.Json;
using System.Text.RegularExpressions;
using LangExtract.Core.Exceptions;
using LangExtract.Core.Schema;

namespace LangExtract.Core;

public class FormatHandler
{
    public FormatMode FormatType { get; }
    public bool UseWrapper { get; }
    public string? WrapperKey { get; }
    public bool UseFences { get; }
    public string AttributeSuffix { get; }
    public bool StrictFences { get; }
    public bool AllowTopLevelList { get; }

    private const string JsonFormat = "json";
    private const string YamlFormat = "yaml";
    private const string YmlFormat = "yml";
        
    private static readonly Regex FenceRegex = new Regex(
        @"```(?<lang>[A-Za-z0-9_+-]+)?(?:\s*\n)?(?<body>[\s\S]*?)```", 
        RegexOptions.Compiled | RegexOptions.Multiline);

    private static readonly Regex ThinkTagRegex = new Regex(@"<think>[\s\S]*?</think>\s*", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public FormatHandler(
        FormatMode formatType = FormatMode.Json,
        bool useWrapper = true,
        string? wrapperKey = null,
        bool useFences = true,
        string attributeSuffix = "_attributes", // Default from data.py in python
        bool strictFences = false,
        bool allowTopLevelList = true)
    {
        FormatType = formatType;
        UseWrapper = useWrapper;
        WrapperKey = useWrapper ? (wrapperKey ?? "extractions") : null;
        UseFences = useFences;
        AttributeSuffix = attributeSuffix;
        StrictFences = strictFences;
        AllowTopLevelList = allowTopLevelList;
    }

    public string FormatExtractionExample(List<Extraction> extractions)
    {
        var items = new List<Dictionary<string, object>>();
        foreach (var ext in extractions)
        {
            var item = new Dictionary<string, object>
            {
                { ext.ExtractionClass, ext.ExtractionText }
            };
            if (ext.Attributes != null && ext.Attributes.Count > 0)
            {
                item[$"{ext.ExtractionClass}{AttributeSuffix}"] = ext.Attributes;
            }
            items.Add(item);
        }

        object payload;
        if (UseWrapper && WrapperKey != null)
        {
            payload = new Dictionary<string, object> { { WrapperKey, items } };
        }
        else
        {
            payload = items;
        }

        string formatted;
        if (FormatType == FormatMode.Yaml)
        {
            // YAML support can be added with libraries like YamlDotNet.
            // For now, focusing on JSON or throwing/mocking.
            throw new NotImplementedException("YAML formatting is not yet supported in this port.");
        }
        else
        {
            formatted = JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping });
        }

        return UseFences ? AddFences(formatted) : formatted;
    }

    public object ParseOutput(string text, bool strict = false)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new FormatError("Empty or invalid input string.");
        }

        string content = ExtractContent(text);
            
        object? parsed = ParseWithFallback(content, strict);

        if (parsed == null)
        {
            if (UseWrapper) throw new FormatError($"Content must be a mapping with an '{WrapperKey}' key.");
            else throw new FormatError("Content must be a list of extractions or a dict.");
        }

        // In C# specific logic, parsed is likely JsonElement or Dictionary/List.
        // Let's assume ParseWithFallback returns JsonElement for now since System.Text.Json is native.
            
        return parsed; // Returning raw parsed object/element to let Resolver handle strong typing if needed
        // Or we can refine return type here. Python returns List<dict>.
    }

    // Specifically returns List<Dictionary<string, object>> equivalent
    public List<Dictionary<string, object>> ParseOutputToList(string text, bool strict = false)
    {
        var parsed = ParseOutput(text, strict);
             
        // Convert to standardized list
        // Logic mirrors parse_output structure validation in Python
             
        JsonElement root;
        if (parsed is JsonElement je) root = je;
        else throw new FormatError($"Unexpected parsed type: {parsed?.GetType()}");

        bool requireWrapper = WrapperKey != null && (UseWrapper || strict);
        JsonElement itemsElement = root;

        if (root.ValueKind == JsonValueKind.Object)
        {
            if (requireWrapper)
            {
                if (!root.TryGetProperty(WrapperKey, out itemsElement))
                {
                    throw new FormatError($"Content must contain an '{WrapperKey}' key.");
                }
            }
            else
            {
                if (root.TryGetProperty("extractions", out var extProp)) itemsElement = extProp;
                else if (WrapperKey != null && root.TryGetProperty(WrapperKey, out var wrapProp)) itemsElement = wrapProp;
                else 
                {
                    // Single item treated as list? Python code does: items = [parsed]
                    // But if parsed is a dict (Object), it wraps it.
                    // We need to re-wrap.
                    // For JsonElement, we can't easily "wrap" without creating a new structure.
                    // Let's defer extraction.
                    return new List<Dictionary<string, object>> { JsonElementToDict(root) };
                }
            }
        }
        else if (root.ValueKind == JsonValueKind.Array)
        {
            if (requireWrapper && (strict || !AllowTopLevelList))
            {
                throw new FormatError($"Content must be a mapping with an '{WrapperKey}' key.");
            }
            if (strict && UseWrapper)
            {
                throw new FormatError("Strict mode requires a wrapper object.");
            }
            if (!AllowTopLevelList)
            {
                throw new FormatError("Top-level list is not allowed.");
            }
            itemsElement = root;
        }
        else
        {
            throw new FormatError($"Expected list or dict, got {root.ValueKind}");
        }

        if (itemsElement.ValueKind != JsonValueKind.Array)
        {
            // Could be single item if we fell through above logic
            if (itemsElement.ValueKind == JsonValueKind.Object)
                return new List<Dictionary<string, object>> { JsonElementToDict(itemsElement) };

            throw new FormatError("The extractions must be a sequence (list) of mappings.");
        }

        var result = new List<Dictionary<string, object>>();
        foreach (var item in itemsElement.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object)
            {
                throw new FormatError("Each item in the sequence must be a mapping.");
            }
            result.Add(JsonElementToDict(item));
        }

        return result;
    }

    private Dictionary<string, object> JsonElementToDict(JsonElement element)
    {
        var dict = new Dictionary<string, object>();
        foreach (var prop in element.EnumerateObject())
        {
            dict[prop.Name] = ConvertJsonElement(prop.Value);
        }
        return dict;
    }

    private object ConvertJsonElement(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object: return JsonElementToDict(element);
            case JsonValueKind.Array: 
                var list = new List<object>();
                foreach (var item in element.EnumerateArray()) list.Add(ConvertJsonElement(item));
                return list;
            case JsonValueKind.String: return element.GetString();
            case JsonValueKind.Number: 
                if (element.TryGetInt32(out int i)) return i;
                if (element.TryGetDouble(out double d)) return d;
                return element.ToString();
            case JsonValueKind.True: return true;
            case JsonValueKind.False: return false;
            case JsonValueKind.Null: return null;
            default: return element.ToString();
        }
    }

    private object? ParseWithFallback(string content, bool strict)
    {
        try
        {
            if (FormatType == FormatMode.Yaml)
            {
                throw new NotImplementedException("YAML parsing not implemented.");
            }
            return JsonDocument.Parse(content).RootElement.Clone(); // Clone to detach
        }
        catch (JsonException)
        {
            if (strict) throw;
                
            // Fallback for <think> tags (Reasoning models)
            var match = ThinkTagRegex.Match(content);
            if (match.Success)
            {
                string stripped = ThinkTagRegex.Replace(content, "").Trim();
                try 
                {
                    return JsonDocument.Parse(stripped).RootElement.Clone();
                }
                catch (JsonException) { /* ignore to throw original or null? */ }
            }
            throw;
        }
    }

    private string ExtractContent(string text)
    {
        if (!UseFences) return text.Trim();

        var matches = FenceRegex.Matches(text);
        var candidates = new List<Match>();

        foreach (Match m in matches)
        {
            if (IsValidLanguageTag(m.Groups["lang"].Value))
            {
                candidates.Add(m);
            }
        }

        if (StrictFences)
        {
            if (candidates.Count != 1)
            {
                if (candidates.Count == 0) throw new FormatError("Input string does not contain valid fence markers.");
                else throw new FormatError("Multiple fenced blocks found. Expected exactly one.");
            }
            return candidates[0].Groups["body"].Value.Trim();
        }

        if (candidates.Count == 1) return candidates[0].Groups["body"].Value.Trim();
        if (candidates.Count > 1) throw new FormatError("Multiple fenced blocks found. Expected exactly one.");

        if (matches.Count > 0)
        {
            if (!StrictFences && matches.Count == 1) return matches[0].Groups["body"].Value.Trim();
            throw new FormatError($"No {FormatType} code block found.");
        }

        return text.Trim();
    }

    private bool IsValidLanguageTag(string? lang)
    {
        if (string.IsNullOrWhiteSpace(lang)) return true;
        lang = lang.Trim().ToLowerInvariant();
        if (FormatType == FormatMode.Json) return lang == "json";
        if (FormatType == FormatMode.Yaml) return lang == "yaml" || lang == "yml";
        return false;
    }

    private string AddFences(string content)
    {
        string fenceType = FormatType == FormatMode.Json ? "json" : "yaml";
        return $"```{fenceType}\n{content.Trim()}\n```";
    }
}