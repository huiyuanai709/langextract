using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using LangExtract.Core;

namespace LangExtract.Logic.Visualization;

public static class Visualizer
{
    private static readonly List<string> Palette = new List<string>
    {
        "#D2E3FC", // Light Blue (Primary Container)
        "#C8E6C9", // Light Green (Tertiary Container)
        "#FEF0C3", // Light Yellow (Primary Color)
        "#F9DEDC", // Light Red (Error Container)
        "#FFDDBE", // Light Orange (Tertiary Container)
        "#EADDFF", // Light Purple (Secondary/Tertiary Container)
        "#C4E9E4", // Light Teal (Teal Container)
        "#FCE4EC", // Light Pink (Pink Container)
        "#E8EAED", // Very Light Grey (Neutral Highlight)
        "#DDE8E8", // Pale Cyan (Cyan Container)
    };

    private const string VisualizationCss = @"
    <style>
    .lx-highlight { position: relative; border-radius:3px; padding:1px 2px;}
    .lx-highlight .lx-tooltip {
      visibility: hidden;
      opacity: 0;
      transition: opacity 0.2s ease-in-out;
      background: #333;
      color: #fff;
      text-align: left;
      border-radius: 4px;
      padding: 6px 8px;
      position: absolute;
      z-index: 1000;
      bottom: 125%;
      left: 50%;
      transform: translateX(-50%);
      font-size: 12px;
      max-width: 240px;
      white-space: normal;
      box-shadow: 0 2px 6px rgba(0,0,0,0.3);
    }
    .lx-highlight:hover .lx-tooltip { visibility: visible; opacity:1; }
    .lx-animated-wrapper { max-width: 100%; font-family: Arial, sans-serif; }
    .lx-controls {
      background: #fafafa; border: 1px solid #90caf9; border-radius: 8px;
      padding: 12px; margin-bottom: 16px;
    }
    .lx-button-row {
      display: flex; justify-content: center; gap: 8px; margin-bottom: 12px;
    }
    .lx-control-btn {
      background: #4285f4; color: white; border: none; border-radius: 4px;
      padding: 8px 16px; cursor: pointer; font-size: 13px; font-weight: 500;
      transition: background-color 0.2s;
    }
    .lx-control-btn:hover { background: #3367d6; }
    .lx-progress-container {
      margin-bottom: 8px;
    }
    .lx-progress-slider {
      width: 100%; margin: 0; appearance: none; height: 6px;
      background: #ddd; border-radius: 3px; outline: none;
    }
    .lx-progress-slider::-webkit-slider-thumb {
      appearance: none; width: 18px; height: 18px; background: #4285f4;
      border-radius: 50%; cursor: pointer;
    }
    .lx-progress-slider::-moz-range-thumb {
      width: 18px; height: 18px; background: #4285f4; border-radius: 50%;
      cursor: pointer; border: none;
    }
    .lx-status-text {
      text-align: center; font-size: 12px; color: #666; margin-top: 4px;
    }
    .lx-text-window {
      font-family: monospace; white-space: pre-wrap; border: 1px solid #90caf9;
      padding: 12px; max-height: 260px; overflow-y: auto; margin-bottom: 12px;
      line-height: 1.6;
    }
    .lx-attributes-panel {
      background: #fafafa; border: 1px solid #90caf9; border-radius: 6px;
      padding: 8px 10px; margin-top: 8px; font-size: 13px;
    }
    .lx-current-highlight {
      border-bottom: 4px solid #ff4444;
      font-weight: bold;
      animation: lx-pulse 1s ease-in-out;
    }
    @keyframes lx-pulse {
      0% { text-decoration-color: #ff4444; }
      50% { text-decoration-color: #ff0000; }
      100% { text-decoration-color: #ff4444; }
    }
    .lx-legend {
      font-size: 12px; margin-bottom: 8px;
      padding-bottom: 8px; border-bottom: 1px solid #e0e0e0;
    }
    .lx-label {
      display: inline-block;
      padding: 2px 4px;
      border-radius: 3px;
      margin-right: 4px;
      color: #000;
    }
    .lx-attr-key {
      font-weight: 600;
      color: #1565c0;
      letter-spacing: 0.3px;
    }
    .lx-attr-value {
      font-weight: 400;
      opacity: 0.85;
      letter-spacing: 0.2px;
    }

    /* Add optimizations with larger fonts and better readability for GIFs */
    .lx-gif-optimized .lx-text-window { font-size: 16px; line-height: 1.8; }
    .lx-gif-optimized .lx-attributes-panel { font-size: 15px; }
    .lx-gif-optimized .lx-current-highlight { text-decoration-thickness: 4px; }
    </style>";

    private enum TagType
    {
        Start,
        End
    }

    private class SpanPoint
    {
        public int Position { get; }
        public TagType TagType { get; }
        public int SpanIdx { get; }
        public Extraction Extraction { get; }

        public SpanPoint(int position, TagType tagType, int spanIdx, Extraction extraction)
        {
            Position = position;
            TagType = tagType;
            SpanIdx = spanIdx;
            Extraction = extraction;
        }
    }

    private class ExtractionData
    {
        [JsonPropertyName("index")]
        public int Index { get; set; }

        [JsonPropertyName("class")]
        public string Class { get; set; } = "";

        [JsonPropertyName("text")]
        public string Text { get; set; } = "";

        [JsonPropertyName("color")]
        public string Color { get; set; } = "";

        [JsonPropertyName("startPos")]
        public int StartPos { get; set; }

        [JsonPropertyName("endPos")]
        public int EndPos { get; set; }

        [JsonPropertyName("beforeText")]
        public string BeforeText { get; set; } = "";

        [JsonPropertyName("extractionText")]
        public string ExtractionText { get; set; } = "";

        [JsonPropertyName("afterText")]
        public string AfterText { get; set; } = "";

        [JsonPropertyName("attributesHtml")]
        public string AttributesHtml { get; set; } = "";
    }

    private static Dictionary<string, string> AssignColors(List<Extraction> extractions)
    {
        var classes = extractions
            .Where(e => e.CharInterval != null)
            .Select(e => e.ExtractionClass)
            .Distinct()
            .OrderBy(c => c)
            .ToList();

        var colorMap = new Dictionary<string, string>();
        int paletteIndex = 0;

        foreach (var cls in classes)
        {
            colorMap[cls] = Palette[paletteIndex % Palette.Count];
            paletteIndex++;
        }

        return colorMap;
    }

    private static List<Extraction> FilterValidExtractions(List<Extraction> extractions)
    {
        return extractions
            .Where(e => e.CharInterval != null &&
                        e.CharInterval.StartPos.HasValue &&
                        e.CharInterval.EndPos.HasValue)
            .ToList();
    }

    private static string BuildHighlightedText(string text, List<Extraction> extractions, Dictionary<string, string> colorMap)
    {
        var points = new List<SpanPoint>();
        var spanLengths = new Dictionary<int, int>();

        for (var i = 0; i < extractions.Count; i++)
        {
            var extraction = extractions[i];
            if (extraction.CharInterval == null ||
                !extraction.CharInterval.StartPos.HasValue ||
                !extraction.CharInterval.EndPos.HasValue ||
                extraction.CharInterval.StartPos.Value >= extraction.CharInterval.EndPos.Value)
            {
                continue;
            }

            var startPos = extraction.CharInterval.StartPos.Value;
            var endPos = extraction.CharInterval.EndPos.Value;

            points.Add(new SpanPoint(startPos, TagType.Start, i, extraction));
            points.Add(new SpanPoint(endPos, TagType.End, i, extraction));
            spanLengths[i] = endPos - startPos;
        }

        // Sort points
        points.Sort((a, b) =>
        {
            if (a.Position != b.Position)
            {
                return a.Position.CompareTo(b.Position);
            }

            // If positions are equal, prioritize logic for nesting
            int aLength = spanLengths.ContainsKey(a.SpanIdx) ? spanLengths[a.SpanIdx] : 0;
            int bLength = spanLengths.ContainsKey(b.SpanIdx) ? spanLengths[b.SpanIdx] : 0;

            // 1. End tags come before start tags
            if (a.TagType != b.TagType)
            {
                return a.TagType == TagType.End ? -1 : 1;
            }

            if (a.TagType == TagType.End)
            {
                // 2. Among end tags: shorter spans close first
                return aLength.CompareTo(bLength);
            }

            // 3. Among start tags: longer spans open first
            return bLength.CompareTo(aLength);
        });

        var htmlParts = new StringBuilder();
        int cursor = 0;

        foreach (var point in points)
        {
            if (point.Position > cursor)
            {
                htmlParts.Append(Uri.EscapeDataString(text.Substring(cursor, point.Position - cursor)));
                // Note: Python uses html.escape(). C# System.Net.WebUtility.HtmlEncode or similar is better.
                // Correcting to HtmlEncode below.
            }

            // Re-doing the previous append to use correct encoding and logic
        }

        // Rewrite loop for correctness regarding HtmlEncode
        htmlParts.Clear();
        cursor = 0;

        foreach (var point in points)
        {
            if (point.Position > cursor)
            {
                string segment = text.Substring(cursor, point.Position - cursor);
                htmlParts.Append(System.Net.WebUtility.HtmlEncode(segment));
            }

            if (point.TagType == TagType.Start)
            {
                string color = colorMap.ContainsKey(point.Extraction.ExtractionClass)
                    ? colorMap[point.Extraction.ExtractionClass]
                    : "#ffff8d";

                string highlightClass = point.SpanIdx == 0 ? " lx-current-highlight" : "";

                htmlParts.Append(
                    $"<span class=\"lx-highlight{highlightClass}\" data-idx=\"{point.SpanIdx}\" style=\"background-color:{color};\">");
            }
            else
            {
                htmlParts.Append("</span>");
            }

            cursor = point.Position;
        }

        if (cursor < text.Length)
        {
            htmlParts.Append(System.Net.WebUtility.HtmlEncode(text.Substring(cursor)));
        }

        return htmlParts.ToString();
    }

    private static string BuildLegendHtml(Dictionary<string, string> colorMap)
    {
        if (colorMap == null || colorMap.Count == 0) return "";

        var legendItems = new List<string>();
        foreach (var kvp in colorMap)
        {
            string cls = System.Net.WebUtility.HtmlEncode(kvp.Key);
            legendItems.Add($"<span class=\"lx-label\" style=\"background-color:{kvp.Value};\">{cls}</span>");
        }

        return $"<div class=\"lx-legend\">Highlights Legend: {string.Join(" ", legendItems)}</div>";
    }

    private static string FormatAttributes(Dictionary<string, object>? attributes)
    {
        if (attributes == null || attributes.Count == 0) return "{}";

        var validAttrs = attributes
            .Where(kvp => kvp.Value != null && kvp.Value.ToString() != "" && kvp.Value.ToString() != "null")
            .ToDictionary(k => k.Key, v => v.Value);

        if (validAttrs.Count == 0) return "{}";

        var attrParts = new List<string>();
        foreach (var kvp in validAttrs)
        {
            string valueStr = kvp.Value.ToString();

            // If it's a list/enumerable, join it
            if (kvp.Value is System.Collections.IEnumerable enumerable && !(kvp.Value is string))
            {
                var items = new List<string>();
                foreach (var item in enumerable) items.Add(item.ToString());
                valueStr = string.Join(", ", items);
            }

            attrParts.Add(
                $"<span class=\"lx-attr-key\">{System.Net.WebUtility.HtmlEncode(kvp.Key)}</span>: <span class=\"lx-attr-value\">{System.Net.WebUtility.HtmlEncode(valueStr)}</span>");
        }

        return "{" + string.Join(", ", attrParts) + "}";
    }

    private static List<ExtractionData> PrepareExtractionData(string text, List<Extraction> extractions, Dictionary<string, string> colorMap, int contextChars = 150)
    {
        var dataList = new List<ExtractionData>();

        for (var i = 0; i < extractions.Count; i++)
        {
            var extraction = extractions[i];
            if (extraction.CharInterval == null ||
                !extraction.CharInterval.StartPos.HasValue ||
                !extraction.CharInterval.EndPos.HasValue)
            {
                continue;
            }

            var startPos = extraction.CharInterval.StartPos.Value;
            var endPos = extraction.CharInterval.EndPos.Value;

            var contextStart = Math.Max(0, startPos - contextChars);
            var contextEnd = Math.Min(text.Length, endPos + contextChars);

            var beforeText = text.Substring(contextStart, startPos - contextStart);
            var extractionText = text.Substring(startPos, endPos - startPos);
            var afterText = text.Substring(endPos, contextEnd - endPos);

            var color = colorMap.GetValueOrDefault(extraction.ExtractionClass, "#ffff8d");

            var attributesHtml = $"<div><strong>class:</strong> {System.Net.WebUtility.HtmlEncode(extraction.ExtractionClass)}</div>";
            attributesHtml += $"<div><strong>attributes:</strong> {FormatAttributes(extraction.Attributes)}</div>";

            dataList.Add(new ExtractionData
            {
                Index = i,
                Class = extraction.ExtractionClass,
                Text = extractionText,
                Color = color,
                StartPos = startPos,
                EndPos = endPos,
                BeforeText = System.Net.WebUtility.HtmlEncode(beforeText),
                ExtractionText = System.Net.WebUtility.HtmlEncode(extractionText),
                AfterText = System.Net.WebUtility.HtmlEncode(afterText),
                AttributesHtml = attributesHtml
            });
        }

        return dataList;
    }

    private static string BuildVisualizationHtml(string text, List<Extraction> extractions, Dictionary<string, string> colorMap, double animationSpeed = 1.0, bool showLegend = true)
    {
        if (extractions.Count == 0)
        {
            return "<div class=\"lx-animated-wrapper\"><p>No extractions to animate.</p></div>";
        }

        // Sort extractions by position and then by length (descending)
        var sortedExtractions = extractions.OrderBy(e => e.CharInterval?.StartPos ?? 0)
            .ThenByDescending(e => (e.CharInterval?.EndPos ?? 0) - (e.CharInterval?.StartPos ?? 0))
            .ToList();

        var highlightedText = BuildHighlightedText(text, sortedExtractions, colorMap);
        var extractionData = PrepareExtractionData(text, sortedExtractions, colorMap);
        var legendHtml = showLegend ? BuildLegendHtml(colorMap) : "";

        var jsData = JsonSerializer.Serialize(extractionData, new JsonSerializerOptions { WriteIndented = false, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping });

        var firstExtraction = extractions[0];
        var posInfoStr = $"[{firstExtraction.CharInterval?.StartPos}-{firstExtraction.CharInterval?.EndPos}]";

        // In C#, we use verbatim strings or string interpolation carefully.
        // Since we have braces for JS code, we need to escape them in interpolation or use Replace.
        // Using string interpolation with double braces for literals.

        var htmlContent = $$"""

                                <div class="lx-animated-wrapper">
                                  <div class="lx-attributes-panel">
                                    {{legendHtml}}
                                    <div id="attributesContainer"></div>
                                  </div>
                                  <div class="lx-text-window" id="textWindow">
                                    {{highlightedText}}
                                  </div>
                                  <div class="lx-controls">
                                    <div class="lx-button-row">
                                      <button class="lx-control-btn" onclick="playPause()">▶️ Play</button>
                                      <button class="lx-control-btn" onclick="prevExtraction()">⏮ Previous</button>
                                      <button class="lx-control-btn" onclick="nextExtraction()">⏭ Next</button>
                                    </div>
                                    <div class="lx-progress-container">
                                      <input type="range" id="progressSlider" class="lx-progress-slider"
                                             min="0" max="{{extractions.Count - 1}}" value="0"
                                             onchange="jumpToExtraction(this.value)">
                                    </div>
                                    <div class="lx-status-text">
                                      Entity <span id="entityInfo">1/{{extractions.Count}}</span> |
                                      Pos <span id="posInfo">{{posInfoStr}}</span>
                                    </div>
                                  </div>
                                </div>

                                <script>
                                  (function() {
                                    const extractions = {{jsData}};
                                    let currentIndex = 0;
                                    let isPlaying = false;
                                    let animationInterval = null;
                                    let animationSpeed = {{animationSpeed}};

                                    function updateDisplay() {
                                      const extraction = extractions[currentIndex];
                                      if (!extraction) return;

                                      document.getElementById('attributesContainer').innerHTML = extraction.attributesHtml;
                                      document.getElementById('entityInfo').textContent = (currentIndex + 1) + '/' + extractions.length;
                                      document.getElementById('posInfo').textContent = '[' + extraction.startPos + '-' + extraction.endPos + ']';
                                      document.getElementById('progressSlider').value = currentIndex;

                                      const playBtn = document.querySelector('.lx-control-btn');
                                      if (playBtn) playBtn.textContent = isPlaying ? '⏸ Pause' : '▶️ Play';

                                      const prevHighlight = document.querySelector('.lx-text-window .lx-current-highlight');
                                      if (prevHighlight) prevHighlight.classList.remove('lx-current-highlight');
                                      const currentSpan = document.querySelector('.lx-text-window span[data-idx="' + currentIndex + '"]');
                                      if (currentSpan) {
                                        currentSpan.classList.add('lx-current-highlight');
                                        currentSpan.scrollIntoView({block: 'center', behavior: 'smooth'});
                                      }
                                    }

                                    function nextExtraction() {
                                      currentIndex = (currentIndex + 1) % extractions.length;
                                      updateDisplay();
                                    }

                                    function prevExtraction() {
                                      currentIndex = (currentIndex - 1 + extractions.length) % extractions.length;
                                      updateDisplay();
                                    }

                                    function jumpToExtraction(index) {
                                      currentIndex = parseInt(index);
                                      updateDisplay();
                                    }

                                    function playPause() {
                                      if (isPlaying) {
                                        clearInterval(animationInterval);
                                        isPlaying = false;
                                      } else {
                                        animationInterval = setInterval(nextExtraction, animationSpeed * 1000);
                                        isPlaying = true;
                                      }
                                      updateDisplay();
                                    }

                                    window.playPause = playPause;
                                    window.nextExtraction = nextExtraction;
                                    window.prevExtraction = prevExtraction;
                                    window.jumpToExtraction = jumpToExtraction;

                                    updateDisplay();
                                  })();
                                </script>
                            """;

        return htmlContent;
    }

    public static string Visualize(AnnotatedDocument doc, double animationSpeed = 1.0, bool showLegend = true, bool gifOptimized = true)
    {
        if (doc == null || string.IsNullOrEmpty(doc.Text))
        {
            throw new ArgumentException("AnnotatedDocument must contain text to visualize.");
        }

        if (doc.Extractions == null)
        {
            throw new ArgumentException("AnnotatedDocument must contain extractions to visualize.");
        }

        var validExtractions = FilterValidExtractions(doc.Extractions);

        if (validExtractions.Count == 0)
        {
            return VisualizationCss + "<div class=\"lx-animated-wrapper\"><p>No valid extractions to animate.</p></div>";
        }

        var colorMap = AssignColors(validExtractions);
        var visualizationHtml = BuildVisualizationHtml(doc.Text, validExtractions, colorMap, animationSpeed, showLegend);

        var fullHtml = VisualizationCss + visualizationHtml;

        if (gifOptimized)
        {
            fullHtml = fullHtml.Replace("class=\"lx-animated-wrapper\"", "class=\"lx-animated-wrapper lx-gif-optimized\"");
        }

        return fullHtml;
    }
}