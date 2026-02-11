using System.Collections.Generic;

namespace LangExtract.Core.Schema
{
    public enum FormatMode
    {
        Json,
        Yaml
    }

    public class FormatModeSchema : BaseSchema
    {
        public FormatMode Format { get; set; }

        public FormatModeSchema(FormatMode format = FormatMode.Json)
        {
            Format = format;
        }

        public override bool RequiresRawOutput => Format == FormatMode.Json;

        public override Dictionary<string, object> ToProviderConfig()
        {
            return new Dictionary<string, object>
            {
                { "format", Format == FormatMode.Json ? "json" : "yaml" }
            };
        }
    }
}
