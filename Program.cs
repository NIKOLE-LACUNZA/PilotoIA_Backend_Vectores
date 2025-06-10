using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using DotNetEnv; 
using Proyecto_IA.Models;
using Proyecto_IA.Data;  // Para DocumentoStore
using System.Threading.Tasks;

var builder = WebApplication.CreateBuilder(args);

Env.Load();

builder.Configuration["AzureBlobStorage:ConnectionString"] =
    Environment.GetEnvironmentVariable("AZURE_STORAGE");

builder.Configuration["OpenAI:ApiKey"] =
    Environment.GetEnvironmentVariable("OPENAI_API_KEY");

builder.Services.AddControllers();
builder.Services.AddSingleton<AzureBlobService>();
builder.Services.AddSingleton<PdfReaderService>();
builder.Services.AddSingleton<VectorStore>();
builder.Services.AddSingleton<ChatService>();

var app = builder.Build();

app.UseAuthorization();
app.MapControllers();

await CargarDocumentosDesdeAzureAsync(app.Services);

app.Run();

async Task CargarDocumentosDesdeAzureAsync(IServiceProvider services)
{
    var blobService = services.GetRequiredService<AzureBlobService>();
    var pdfReader = services.GetRequiredService<PdfReaderService>();

    var archivos = await blobService.ListarArchivosAsync();
    
    foreach (var archivo in archivos)
    {
        string nombreArchivo = archivo.Nombre;

        if (!nombreArchivo.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
            continue;

        var blobClient = blobService.ContainerClient.GetBlobClient(nombreArchivo);

        var downloadResponse = await blobClient.DownloadAsync();
        using var originalStream = downloadResponse.Value.Content;

        using var memoryStream = new MemoryStream();
        await originalStream.CopyToAsync(memoryStream);
        memoryStream.Position = 0;

        var formFile = new FormFile(memoryStream, 0, memoryStream.Length, "file", nombreArchivo)
        {
            Headers = new HeaderDictionary(),
            ContentType = "application/pdf"
        };

        string texto = await pdfReader.ExtractTextAsync(formFile);

        DocumentoStore.Documentos.Add(new DocumentoInfo
        {
            Nombre = Path.GetFileName(archivo.Nombre),
            Url = archivo.Url,
            Texto = texto
        });
    }
}
