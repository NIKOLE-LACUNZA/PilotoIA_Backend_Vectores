using Microsoft.AspNetCore.Mvc;
using Proyecto_IA.Models;
using Proyecto_IA.Data;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System;

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

        
        if (DocumentoStore.Documentos.Any(d => d.Nombre.Equals(archivo.NombreArchivo, StringComparison.OrdinalIgnoreCase)))
        {
            return Conflict(new { mensaje = "Nombre de archivo existente, por favor ponga otro." });
        }

        byte[] fileBytes;
        try
        {
            fileBytes = Convert.FromBase64String(archivo.Base64Contenido);
        }
        catch
        {
            return BadRequest("Base64 inválido.");
        }

        using var stream = new MemoryStream(fileBytes);
        var formFile = new FormFile(stream, 0, fileBytes.Length, "file", archivo.NombreArchivo)
        {
            Headers = new HeaderDictionary(),
            ContentType = "application/pdf"
        };

        
        var urlBlob = await _blobService.UploadFileAsync(formFile);

        stream.Position = 0;
        var texto = await _pdfReader.ExtractTextAsync(formFile);

        
        await _chatService.ProcesarDocumento(texto, archivo.NombreArchivo);
        var vectorJson = _chatService.ObtenerVectorComoJson(archivo.NombreArchivo);
        var nombreVector = Path.GetFileNameWithoutExtension(archivo.NombreArchivo) + ".vector.json";
        var vectorUrl = await _blobService.UploadTextAsync(nombreVector, vectorJson);
        await _blobService.UploadTextAsync(nombreVector, vectorJson);

        
        
        DocumentoStore.Documentos.Add(new DocumentoInfo
        {
            Nombre = Path.GetFileName(archivo.NombreArchivo),
            Url = urlBlob,
            Texto = texto
        });

        return Ok(new
        {
            documento = urlBlob,
            vector = vectorUrl 
        });
    }


    [HttpGet("list")]
public async Task<IActionResult> ListarDocumentosAzure()
{
    var archivos = await _blobService.ListarArchivosAsync();
    return Ok(archivos.Select(a => new { Nombre = a.Nombre, Url = a.Url }));
}

[HttpPost("preguntar-grupo")]
public async Task<IActionResult> PreguntarGrupo([FromBody] PreguntaGrupoRequest request)
{
    var documentos = DocumentoStore.Documentos
        .Where(d => request.NombresDocumentos.Contains(d.Nombre))
        .ToList();

    if (!documentos.Any())
        return NotFound("No se encontraron los documentos indicados.");

    string textoCompleto = "";

    foreach (var doc in documentos)
    {
        
        string nombreVector = System.IO.Path.GetFileNameWithoutExtension(doc.Nombre) + ".vector.json";

        
        string vectorTexto = await _blobService.LeerArchivoTextoAsync(nombreVector);

        textoCompleto += vectorTexto + "\n\n";
    }

await _chatService.ProcesarDocumento(textoCompleto, "documento-combinado");

    var respuesta = await _chatService.ConsultarPregunta(request.Pregunta, "documento-combinado");

    return Ok(new { respuesta });
}


}
