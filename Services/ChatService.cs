using OpenAI_API;
using OpenAI_API.Chat;

public class ChatService
{
    private readonly VectorStore _vectorStore;
    private readonly OpenAIAPI _api;

    public ChatService(IConfiguration config, VectorStore vectorStore)
    {
        _vectorStore = vectorStore;
        _api = new OpenAIAPI(config["OpenAI:ApiKey"]);
    }

    // Procesar un documento y asociarlo con su nombre
    public async Task ProcesarDocumento(string texto, string nombreDocumento)
    {
        var fragmentos = texto.Split(new[] { "\n\n", "\r\n\r\n" }, StringSplitOptions.RemoveEmptyEntries);
        foreach (var f in fragmentos)
        {
            await _vectorStore.AgregarTextoAsync(f, nombreDocumento);
        }
    }

    // Consultar pregunta asociada a un documento específico
    public async Task<string> ConsultarPregunta(string pregunta, string nombreDocumento)
    {
        var contexto = _vectorStore.BuscarContexto(pregunta, nombreDocumento);

        var chat = _api.Chat.CreateConversation();
        chat.AppendSystemMessage("Responde usando solo este contexto:");
        chat.AppendUserInput(contexto);
        chat.AppendUserInput(pregunta);

        return await chat.GetResponseFromChatbotAsync();
    }
public string ObtenerVectorComoJson(string nombreDocumento)
{
    return _vectorStore.ObtenerVectorComoJson(nombreDocumento);
}
}
