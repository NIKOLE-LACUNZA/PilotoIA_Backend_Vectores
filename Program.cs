using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using DotNetEnv; 

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
app.Run();