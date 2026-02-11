using System.Collections.Generic;

namespace LangExtract.Core
{
    /// <summary>
    /// Represents an extraction extracted from text.
    /// </summary>
    public class Extraction
    {
        public string ExtractionClass { get; set; }
        public string ExtractionText { get; set; }
        public CharInterval? CharInterval { get; set; }
        public AlignmentStatus? AlignmentStatus { get; set; }
        public int? ExtractionIndex { get; set; }
        public int? GroupIndex { get; set; }
        public string? Description { get; set; }
        public TokenInterval? TokenInterval { get; set; }
        public Dictionary<string, object>? Attributes { get; set; } // Using object to support string or list<string>

        public Extraction(
            string extractionClass, 
            string extractionText, 
            CharInterval? charInterval = null,
            AlignmentStatus? alignmentStatus = null,
            int? extractionIndex = null,
            int? groupIndex = null,
            string? description = null,
            Dictionary<string, object>? attributes = null)
        {
            ExtractionClass = extractionClass;
            ExtractionText = extractionText;
            CharInterval = charInterval;
            AlignmentStatus = alignmentStatus;
            ExtractionIndex = extractionIndex;
            GroupIndex = groupIndex;
            Description = description;
            Attributes = attributes;
        }
    }
}
