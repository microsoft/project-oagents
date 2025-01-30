using System.Reflection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Memory;
using Microsoft.SemanticKernel.Connectors.AzureAISearch;
using Azure.AI.DocumentIntelligence;
using Azure;

namespace SupportCenter.Invoice.Memory;

internal class MemoryProgram
{
    static string[] files = { "https://github.com/Azure-Samples/cognitive-services-REST-api-samples/raw/master/curl/form-recognizer/rest-api/invoice.pdf" };
    static async Task Main(string[] args)
    {
        var kernelSettings = KernelSettings.LoadSettings();
        string endpoint = kernelSettings.DocumentIntelligenceEndpoint;
        string key = kernelSettings.DocumentIntelligenceKey;
        AzureKeyCredential credential = new AzureKeyCredential(key);
        DocumentIntelligenceClient client = new DocumentIntelligenceClient(new Uri(endpoint), credential);
        Console.WriteLine($"DocumentIntelligence endpoint {endpoint}");
        Console.WriteLine($"Search endpoint {kernelSettings.SearchEndpoint}");

        using ILoggerFactory loggerFactory = LoggerFactory.Create(builder =>
        {
            builder
                .SetMinimumLevel(kernelSettings.LogLevel ?? LogLevel.Debug)
                .AddConsole()
                .AddDebug();
        });

        var memoryBuilder = new MemoryBuilder();
        var memory = memoryBuilder.WithLoggerFactory(loggerFactory)
                    .WithMemoryStore(new AzureAISearchMemoryStore(kernelSettings.SearchEndpoint, kernelSettings.SearchKey))
                    .WithAzureOpenAITextEmbeddingGeneration(kernelSettings.EmbeddingDeploymentOrModelId, kernelSettings.Endpoint, kernelSettings.ApiKey)
                    .Build();

        foreach (var file in files)
        {
            Console.WriteLine($"file {file}");
            AnalyzeResult result = await AnalyzeDoc(client, file);
            await ImportDocumentAsync(memory, kernelSettings.SearchIndex, result.Content);
            //Thread.Sleep(60000); //throttled to 1 request per minute
        }
    }

    public static async Task ImportDocumentAsync(ISemanticTextMemory memory, string collection, string text)
    {
        Console.WriteLine($"text: {text}");
        Console.WriteLine($"collection: {collection}");
        try
        {
            var descr = text.Take(100);
            await memory.SaveInformationAsync(
                collection: collection,
                text: text,
                id: $"{Guid.NewGuid()}",
                description: $"{descr}");
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }

    static async Task<AnalyzeResult> AnalyzeDoc(DocumentIntelligenceClient client, string invoice, string modelName = "prebuilt-invoice")
    {

        var currentDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        Uri invoiceUri = new Uri(invoice);

        Console.WriteLine($"invoiceUri {invoiceUri}");
        AnalyzeDocumentContent content = new AnalyzeDocumentContent()
        {
            UrlSource = invoiceUri
        };

        Operation<AnalyzeResult> operation = await client.AnalyzeDocumentAsync(WaitUntil.Completed, "prebuilt-invoice", content);
        return operation.Value;
    }
}