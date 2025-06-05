
using OpenAI_API;

public class VectorStore
{
    private List<(string texto, float[] embedding)> _documentos = new();
    private readonly OpenAIAPI _api;

    public VectorStore(IConfiguration config)
    {
        _api = new OpenAIAPI(config["OpenAI:ApiKey"]);
    }

    public async Task AgregarTextoAsync(string texto)
    {
        var embedding = await _api.Embeddings.CreateEmbeddingAsync(texto);
        _documentos.Add((texto, embedding.Data[0].Embedding));
    }

    public string BuscarContexto(string pregunta)
    {
        var embPregunta = _api.Embeddings.CreateEmbeddingAsync(pregunta).Result.Data[0].Embedding;
        return _documentos
            .OrderByDescending(d => CosineSimilarity(d.embedding, embPregunta))
            .Take(3)
            .Select(d => d.texto)
            .Aggregate((a, b) => a + "\n" + b);
    }

    private static float CosineSimilarity(float[] v1, float[] v2)
    {
        float dot = 0, mag1 = 0, mag2 = 0;
        for (int i = 0; i < v1.Length; i++)
        {
            dot += v1[i] * v2[i];
            mag1 += v1[i] * v1[i];
            mag2 += v2[i] * v2[i];
        }
        return dot / ((float)Math.Sqrt(mag1) * (float)Math.Sqrt(mag2));
    }
}
