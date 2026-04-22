using PDFtoImage;
using SkiaSharp;
using LangExtract.Providers;
using Microsoft.Extensions.AI;

namespace LangExtract.TestApp;

public static class PdfOcrPipeline
{
    public const string OcrPrompt = """
你是一个专业的文档 OCR 助手。
我会给你一张 PDF 页面的图片，请将图中所有文字内容完整转录出来。

要求：
- 保持原文，不要改写、不要总结
- 保留段落结构，段落之间用空行分隔
- 表格内容用 | 竖线格式保留列结构
- 页码、页眉、页脚放在最后，标注 [页眉] [页脚]
- 无法识别的字符用 [?] 占位
- 只输出转录文本，不要加任何解释
""";

    public static async Task<(List<string> PageImagePaths, string CombinedOcrText)> RunAsync(
        string pdfPath,
        string outputDir,
        OpenAIProvider visionModelProvider,
        CancellationToken cancellationToken)
    {
        if (!File.Exists(pdfPath))
            throw new FileNotFoundException("PDF 文件不存在", pdfPath);

        Directory.CreateDirectory(outputDir);

        // var pageImagePaths = await ConvertPdfToPngPages(pdfPath, outputDir, dpi: 200);

        var pageImagePaths = new List<string>()
        {
            $"{Path.Combine(outputDir, "page_001.png")}"
        };

        var perPageOcr = new List<string>(capacity: pageImagePaths.Count);
        for (var i = 0; i < pageImagePaths.Count; i++)
        {
            var pageNo = i + 1;
            var imgPath = pageImagePaths[i];

            var ocrText = await OcrSinglePageAsync(visionModelProvider, imgPath, cancellationToken);
            perPageOcr.Add($"[第{pageNo}页]\n{ocrText}".Trim());
        }

        var combinedOcrText = string.Join("\n\n", perPageOcr);


        return (pageImagePaths, combinedOcrText);
    }

    private static async Task<List<string>> ConvertPdfToPngPages(string pdfPath, string outputDir, int dpi)
    {
        var options = new RenderOptions(Dpi: dpi);
        var paths = new List<string>();

        // 逐页渲染并保存为 page_001.png / page_002.png ...
        var index = 0;
        await foreach (var bitmap in Conversion.ToImagesAsync(File.OpenRead(pdfPath), .., options: options).ConfigureAwait(false))
        {
            var fileName = $"page_{index + 1:000}.png";
            var path = Path.Combine(outputDir, fileName);
            await using var fs = File.Open(path, FileMode.Create, FileAccess.Write, FileShare.None);
            bitmap.Encode(fs, SKEncodedImageFormat.Png, 100);
            paths.Add(path);
            index++;
        }
        if (paths.Count == 0)
            throw new InvalidOperationException("未能从 PDF 渲染出任何页面图片");

        return paths;
    }

    private static async Task<string> OcrSinglePageAsync(
        OpenAIProvider visionModelProvider,
        string imagePath,
        CancellationToken cancellationToken)
    {
        var bytes = await File.ReadAllBytesAsync(imagePath, cancellationToken);

        var messages = new List<ChatMessage>
        {
            new(ChatRole.User,
            [
                new TextContent(OcrPrompt),
                new DataContent(bytes, "image/png"),
            ])
        };

        var options = new ChatOptions { Temperature = 0 };
        return await visionModelProvider.InferAsync(messages, options, cancellationToken);
    }
}

