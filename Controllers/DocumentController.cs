using Microsoft.AspNetCore.Mvc;

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

    [HttpPost("upload")]
    public IActionResult TestUpload()
    {
        return Ok("Recibido");
    }

    [HttpGet("ping")]
    public IActionResult Ping()
    {
        return Ok("DocumentController está activo");
    }

    /* //[HttpPost("upload")]
    public async Task<IActionResult> Upload(IFormFile file)
    {
        var blobUrl = await _blobService.UploadFileAsync(file);
        var text = await _pdfReader.ExtractTextAsync(file);
        await _chatService.ProcesarDocumento(text);
        return Ok(new { url = blobUrl });
    } */

    [HttpPost("preguntar")]
    public async Task<IActionResult> Preguntar([FromBody] string pregunta)
    {
        var respuesta = await _chatService.ConsultarPregunta(pregunta);
        return Ok(new { respuesta });
    }
}
