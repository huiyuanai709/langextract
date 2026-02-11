using System.Collections.Generic;

namespace LangExtract.Core.Schema
{
    public abstract class BaseSchema
    {
        public abstract bool RequiresRawOutput { get; }
        public abstract Dictionary<string, object> ToProviderConfig();
    }
}
