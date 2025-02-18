using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel.Connectors.Redis;
using SupportCenter.Shared;
using OpenAI;
using StackExchange.Redis;
using System.Reflection;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;

string[] files = [
    "Benefit_Options.pdf", 
    "employee_handbook.pdf", 
    "Northwind_Health_Plus_Benefits_Details.pdf", 
    "Northwind_Standard_Benefits_Details.pdf", 
    "role_library.pdf"
];

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

builder.AddAzureOpenAIClient("openAiConnection");
builder.AddRedisClient("redis");
builder.Services.AddSingleton<IVectorStore>(sp => {
    var db = sp.GetRequiredService<IConnectionMultiplexer>().GetDatabase();
    return new RedisVectorStore(db, new() { StorageType = RedisStorageType.HashSet });
});

builder.Services.AddEmbeddingGenerator(s => {
    return s.GetRequiredService<OpenAIClient>().AsEmbeddingGenerator("text-embedding-3-large");
});

using IHost host = builder.Build();

foreach (var file in files)
{
    await ImportDocumentAsync(host.Services, file);
}

await host.RunAsync();


static async Task ImportDocumentAsync(IServiceProvider hostProvider,  string filename)
{
    using IServiceScope serviceScope = hostProvider.CreateScope();
    var provider = serviceScope.ServiceProvider;
    var vectorStore = provider.GetRequiredService<IVectorStore>();
    var embeddingGenerator = provider.GetRequiredService<IEmbeddingGenerator<string, Embedding<float>>>();

    var currentDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) 
        ?? throw new InvalidOperationException("Current directory cannot be determined.");
    var filePath = Path.Combine(currentDirectory, filename);
    using var pdfDocument = PdfDocument.Open(File.OpenRead(filePath));
    var pages = pdfDocument.GetPages();
    var collection = vectorStore.GetCollection<string, Document>("qna");
    await collection.CreateCollectionIfNotExistsAsync();
    foreach (var page in pages)
    {
        try
        {
            var text = ContentOrderTextExtractor.GetText(page);
            var descr = new string([.. text.Take(100)]);
            var vector = await embeddingGenerator.GenerateEmbeddingVectorAsync(text);
            await collection.UpsertAsync(new Document { Key = $"{Guid.NewGuid()}", Description = $"Document: {descr}", Text = text, Vector = vector });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error processing page: {ex.Message}");
        }
    }
}