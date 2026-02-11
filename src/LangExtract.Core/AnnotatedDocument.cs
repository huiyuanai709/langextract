using System;
using System.Collections.Generic;

namespace LangExtract.Core
{
    /// <summary>
    /// Class for representing annotated documents.
    /// </summary>
    public class AnnotatedDocument
    {
        private string? _documentId;

        public List<Extraction>? Extractions { get; set; }
        public string? Text { get; set; }

        public AnnotatedDocument(string? text = null, string? documentId = null, List<Extraction>? extractions = null)
        {
            Text = text;
            _documentId = documentId;
            Extractions = extractions;
        }

        public string DocumentId
        {
            get
            {
                if (_documentId == null)
                {
                    _documentId = $"doc_{Guid.NewGuid().ToString("N").Substring(0, 8)}";
                }
                return _documentId;
            }
            set => _documentId = value;
        }
    }
}
