namespace LangExtract.Core;

/// <summary>
    /// Represents a value that can be of different types in extraction data.
    /// </summary>
    public class ExtractionValue
    {
        private readonly object? _value;
        private readonly ValueType _type;

        private enum ValueType
        {
            String,
            Int,
            Float,
            Dict,
            Null
        }

        public ExtractionValue(string value)
        {
            _value = value;
            _type = ValueType.String;
        }

        public ExtractionValue(int value)
        {
            _value = value;
            _type = ValueType.Int;
        }

        public ExtractionValue(float value)
        {
            _value = value;
            _type = ValueType.Float;
        }

        public ExtractionValue(Dictionary<string, object> value)
        {
            _value = value;
            _type = ValueType.Dict;
        }

        private ExtractionValue()
        {
            _value = null;
            _type = ValueType.Null;
        }

        public static ExtractionValue Null() => new ExtractionValue();

        public bool IsString => _type == ValueType.String;
        public bool IsInt => _type == ValueType.Int;
        public bool IsFloat => _type == ValueType.Float;
        public bool IsDict => _type == ValueType.Dict;
        public bool IsNull => _type == ValueType.Null;

        public string Type => _type.ToString();

        public string StringValue => IsString ? (string)_value : throw new InvalidOperationException("Value is not a string");
        public int IntValue => IsInt ? (int)_value : throw new InvalidOperationException("Value is not an int");
        public float FloatValue => IsFloat ? (float)_value : throw new InvalidOperationException("Value is not a float");
        public Dictionary<string, object> DictValue => IsDict ? (Dictionary<string, object>)_value : throw new InvalidOperationException("Value is not a dict");

        public override string ToString()
        {
            return _value?.ToString() ?? "null";
        }
    }