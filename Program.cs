using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using DotNetEnv;
using Proyecto_IA.Models;
using Proyecto_IA.Data;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Threading.Tasks;

// Inicializar builder
var builder = WebApplication.CreateBuilder(args);

// Cargar variables de entorno
Env.Load();

// Configurar claves desde variables de entorno
builder.Configuration["AzureBlobStorage:ConnectionString"] =
    Environment.GetEnvironmentVariable("AZURE_STORAGE");

builder.Configuration["OpenAI:ApiKey"] =
    Environment.GetEnvironmentVariable("OPENAI_API_KEY");

// Permitir request de hasta 1 GB
const long oneGigabyte = 1_073_741_824;

builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.Limits.MaxRequestBodySize = oneGigabyte;
});

builder.Services.Configure<IISServerOptions>(options =>
{
    options.MaxRequestBodySize = oneGigabyte;
});

// Servicios
builder.Services.AddControllers();
builder.Services.AddSingleton<AzureBlobService>();
builder.Services.AddSingleton<PdfReaderService>();
builder.Services.AddSingleton<VectorStore>();
builder.Services.AddSingleton<ChatService>();

var app = builder.Build();

app.UseAuthorization();
app.MapControllers();

// Cargar documentos iniciales desde Azure
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
