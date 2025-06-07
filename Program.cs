using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using DotNetEnv; 
using Proyecto_IA.Models;

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

// 🚀 AQUI AGREGAS EL CARGADO DE DOCUMENTOS

var documentosPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "documentos");
var vectoresPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "vectores");

Directory.CreateDirectory(documentosPath);
Directory.CreateDirectory(vectoresPath);

var archivos = Directory.GetFiles(documentosPath);

foreach (var archivoPath in archivos)
{
    var nombreArchivo = Path.GetFileName(archivoPath);

    // Leer el vector si existe (por ahora lo guardamos en Texto)
    var vectorFileName = Path.GetFileNameWithoutExtension(nombreArchivo) + ".vector.json";
    var vectorPath = Path.Combine(vectoresPath, vectorFileName);

    string textoDocumento = "";

    if (System.IO.File.Exists(vectorPath))
    {
        textoDocumento = await System.IO.File.ReadAllTextAsync(vectorPath);
    }

    // Agregar a DocumentoStore
    Proyecto_IA.Data.DocumentoStore.Documentos.Add(new Proyecto_IA.Models.DocumentoInfo
    {
        Nombre = nombreArchivo,
        Url = $"/documentos/{nombreArchivo}",
        Texto = textoDocumento
    });
}

app.Run();
