namespace LangExtract.Core.Exceptions;

public class LangExtractError : Exception
{
    public LangExtractError(string message) : base(message) { }
    public LangExtractError(string message, Exception innerException) : base(message, innerException) { }
}

public class InvalidDatasetError : LangExtractError
{
    public InvalidDatasetError(string message) : base(message) { }
    public InvalidDatasetError(string message, Exception innerException) : base(message, innerException) { }
}

public class InvalidDocumentError : LangExtractError
{
    public InvalidDocumentError(string message) : base(message) { }
}

public class FormatError : LangExtractError
{
    public FormatError(string message) : base(message) { }
    public FormatError(string message, Exception innerException) : base(message, innerException) { }
}

public class ResolverParsingError : LangExtractError
{
    public ResolverParsingError(string message) : base(message) { }
    public ResolverParsingError(string message, Exception innerException) : base(message, innerException) { }
}

public class InferenceOutputError : LangExtractError
{
    public InferenceOutputError(string message) : base(message) { }
}