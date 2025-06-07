using Microsoft.AspNetCore.Mvc;
using Proyecto_IA.Models;
using Proyecto_IA.Data;

[ApiController]
[Route("api/[controller]")]
public class DocumentController : ControllerBase
{
    private readonly AzureBlobService _blobService;
    private readonly PdfReaderService _pdfReader;
    private readonly ChatService _chatService;

    public DocumentController(AzureBlobService blobService, PdfReaderService pdfReader, ChatService chatService)
    {
        _blobService = blobService;
        _pdfReader = pdfReader;
        _chatService = chatService;
    }


    [HttpPost("subir-base64")]
    public async Task<IActionResult> SubirYProcesarBase64([FromBody] ArchivoBase64Request archivo)
    {
        if (archivo == null || string.IsNullOrWhiteSpace(archivo.Base64Contenido) || string.IsNullOrWhiteSpace(archivo.NombreArchivo))
            return BadRequest("Falta nombre o contenido.");


        byte[] fileBytes;
        try
        {
            fileBytes = Convert.FromBase64String(archivo.Base64Contenido);
        }
        catch
        {
            return BadRequest("Base64 inválido.");
        }


        var documentosPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "documentos");
        var vectoresPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "vectores");

        Directory.CreateDirectory(documentosPath);
        Directory.CreateDirectory(vectoresPath);

        var filePath = Path.Combine(documentosPath, archivo.NombreArchivo);
        var vectorFileName = Path.GetFileNameWithoutExtension(archivo.NombreArchivo) + ".vector.json";
        var vectorPath = Path.Combine(vectoresPath, vectorFileName);


        await System.IO.File.WriteAllBytesAsync(filePath, fileBytes);




        _ = Task.Run(async () =>
        {
            try
            {
                using var stream = new MemoryStream(fileBytes);
                var formFile = new FormFile(stream, 0, fileBytes.Length, "file", archivo.NombreArchivo)
                {
                    Headers = new HeaderDictionary(),
                    ContentType = "application/pdf"
                };

                var texto = await _pdfReader.ExtractTextAsync(formFile);
                await _chatService.ProcesarDocumento(texto, archivo.NombreArchivo);


                var vectorJson = _chatService.ObtenerVectorComoJson(archivo.NombreArchivo);
                await System.IO.File.WriteAllTextAsync(vectorPath, vectorJson);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al procesar documento: {ex.Message}");
            }
        });

        var urlDocumento = $"/documentos/{archivo.NombreArchivo}";
        var urlVector = $"/vectores/{vectorFileName}";

        DocumentoStore.Documentos.Add(new DocumentoInfo
        {
            Nombre = archivo.NombreArchivo,
            Url = urlDocumento,
            Texto = ""
        });

        return Ok(new
        {
            documento = urlDocumento,
            vector = urlVector
        });
    }

    [HttpGet("list")]
    public IActionResult ListarDocumentos()
    {
        var lista = DocumentoStore.Documentos
            .Select(d => new { d.Nombre, d.Url })
            .ToList();

        return Ok(lista);
    }

[HttpPost("preguntar-grupo")]
public async Task<IActionResult> PreguntarGrupo([FromBody] PreguntaGrupoRequest request)
{
    var documentos = DocumentoStore.Documentos
        .Where(d => request.NombresDocumentos.Contains(d.Nombre))
        .ToList();

    if (!documentos.Any())
        return NotFound("No se encontraron los documentos indicados.");

    // Combinar todos los textos
    string textoCompleto = string.Join("\n\n", documentos.Select(d => d.Texto));

    // Procesar como "documento combinado"
    await _chatService.ProcesarDocumento(textoCompleto, "documento-combinado");

    var respuesta = await _chatService.ConsultarPregunta(request.Pregunta, "documento-combinado");

    return Ok(new { respuesta });
}

}
