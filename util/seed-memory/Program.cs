using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Text;
using Microsoft.Extensions.Logging;
using System.Text;
using Microsoft.SemanticKernel.Connectors.Memory.Qdrant;
using Microsoft.SemanticKernel.Connectors.AI.OpenAI.TextEmbedding;
using Microsoft.SemanticKernel.Memory;

class Program
{
    static async Task Main(string[] args)
    {
       var kernelSettings = KernelSettings.LoadSettings();

        var kernelConfig = new KernelConfig();

        using ILoggerFactory loggerFactory = LoggerFactory.Create(builder =>
        {
            builder
                .SetMinimumLevel(kernelSettings.LogLevel ?? LogLevel.Warning)
                .AddConsole()
                .AddDebug();
        });
       
        var memoryStore = new QdrantMemoryStore(new QdrantVectorDbClient("http://qdrant", 1536, port: 6333));
        var embedingGeneration = new AzureTextEmbeddingGeneration(kernelSettings.EmbeddingDeploymentOrModelId, kernelSettings.Endpoint, kernelSettings.ApiKey);
        var semanticTextMemory = new SemanticTextMemory(memoryStore, embedingGeneration);

        var kernel = new KernelBuilder()
                            .WithLogger(loggerFactory.CreateLogger<IKernel>())
                            .WithAzureChatCompletionService(kernelSettings.DeploymentOrModelId, kernelSettings.Endpoint, kernelSettings.ApiKey, true, kernelSettings.ServiceId, true)
                            .WithMemory(semanticTextMemory)
                            .WithConfiguration(kernelConfig).Build();
        await ImportDocumentAsync(kernel);
    }

    public static async Task ImportDocumentAsync(IKernel kernel)
        {
            var wafFilePath = "azure-well-architected.pdf";
            var fileContent = ReadPdfFile(wafFilePath);
            await ParseDocumentContentToMemoryAsync(kernel, fileContent, "waf", Guid.NewGuid().ToString());
        }

    private static string ReadPdfFile(string file)
        {
            var sb = new StringBuilder();
            using var pdfDocument = PdfDocument.Open(File.OpenRead(file));
            foreach (var page in pdfDocument.GetPages())
            {
                var text = ContentOrderTextExtractor.GetText(page);
                sb.Append(text);
            }

            return sb.ToString();
        }

    private static async Task ParseDocumentContentToMemoryAsync(IKernel kernel, string content, string documentName, string memorySourceId)
        {
            var lines = TextChunker.SplitPlainTextLines(content, 30);
            var paragraphs = TextChunker.SplitPlainTextParagraphs(lines, 100);

            for (var i = 0; i < paragraphs.Count; i++)
            {
                var paragraph = paragraphs[i];
                await kernel.Memory.SaveInformationAsync(
                    collection: "waf",
                    text: paragraph,
                    id: $"{memorySourceId}_{i}",
                    description: $"Document: {documentName}");
            }
        }
}