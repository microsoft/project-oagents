using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Text;

class Program
{
    static async Task Main(string[] args)
    {
       var kernelSettings = KernelSettings.LoadSettings();

        var kernelConfig = new KernelConfig();
        kernelConfig.AddCompletionBackend(kernelSettings);

        using ILoggerFactory loggerFactory = LoggerFactory.Create(builder =>
        {
            builder
                .SetMinimumLevel(kernelSettings.LogLevel ?? LogLevel.Warning)
                .AddConsole()
                .AddDebug();
        });

        var kernel = new KernelBuilder()
                            .WithLogger(loggerFactory.CreateLogger<IKernel>())
                            .WithConfiguration(kernelConfig).Build();
        await ImportDocumentAsync(kernel);
    }

    public async Task ImportDocumentAsync(IKernel kernel)
        {
            var wafFilePath = "azure-well-architected.pdf";
            var fileContent = ReadPdfFile(formFile);
            await ParseDocumentContentToMemoryAsync(kernel, fileContent, "waf", Guid.NewGuid().ToString());
        }

    private string ReadPdfFile(IFormFile file)
        {
            var fileContent = string.Empty;
            using var pdfDocument = PdfDocument.Open(file.OpenReadStream());
            foreach (var page in pdfDocument.GetPages())
            {
                var text = ContentOrderTextExtractor.GetText(page);
                fileContent += text;
            }

            return fileContent;
        }

    private async Task ParseDocumentContentToMemoryAsync(IKernel kernel, string content, string documentName, string memorySourceId)
        {
            var lines = TextChunker.SplitPlainTextLines(content, this._options.DocumentLineSplitMaxTokens);
            var paragraphs = TextChunker.SplitPlainTextParagraphs(lines, this._options.DocumentParagraphSplitMaxLines);

            for (var i = 0; i < paragraphs.Count; i++)
            {
                var paragraph = paragraphs[i];
                await kernel.Memory.SaveInformationAsync(
                    collection: "waf",
                    text: paragraph,
                    id: $"{memorySourceId}-{i}",
                    description: $"Document: {documentName}");
            }

            this._logger.LogInformation(
                "Parsed {0} paragraphs from local file {1}",
                paragraphs.Count,
                documentName
            );
        }
}