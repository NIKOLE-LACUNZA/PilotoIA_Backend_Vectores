using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

public class AzureBlobService
{
    public string ConnectionString { get; set; }
    public string ContainerName { get; set; }
    private readonly BlobContainerClient _container;
    public BlobContainerClient ContainerClient => _container;
    
    public AzureBlobService(IConfiguration config)
    {
        ConnectionString = config["AzureBlobStorage:ConnectionString"] 
            ?? throw new ArgumentNullException(nameof(config), "ConnectionString no puede ser null");
        ContainerName = config["AzureBlobStorage:ContainerName"] 
            ?? throw new ArgumentNullException(nameof(config), "ContainerName no puede ser null");

        _container = new BlobContainerClient(ConnectionString, ContainerName);
        _container.CreateIfNotExists();
    }

    public async Task<bool> ExisteArchivoAsync(string nombreArchivo)
    {
        var blobClient = _container.GetBlobClient(nombreArchivo);
        return await blobClient.ExistsAsync();
    }

    public async Task<string> UploadFileAsync(IFormFile file)
    {
        var blobClient = _container.GetBlobClient(file.FileName);
        using var stream = file.OpenReadStream();
        await blobClient.UploadAsync(stream, overwrite: false); 
        return blobClient.Uri.ToString();
    }
    public async Task<string> UploadTextAsync(string nombreArchivo, string contenido)
    {
        var blobClient = _container.GetBlobClient(nombreArchivo);
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(contenido));
        await blobClient.UploadAsync(stream, overwrite: true);
        return blobClient.Uri.ToString();
    }
    public async Task<List<(string Nombre, string Url)>> ListarArchivosAsync()
    {
        var lista = new List<(string, string)>();
        await foreach (BlobItem blobItem in _container.GetBlobsAsync())
        {
            var blobClient = _container.GetBlobClient(blobItem.Name);
            lista.Add((blobItem.Name, blobClient.Uri.ToString()));
        }
        return lista;
    }
public async Task<string> LeerArchivoTextoAsync(string nombreArchivo)
{
    var blobClient = _container.GetBlobClient(nombreArchivo);
    if (await blobClient.ExistsAsync())
    {
        var download = await blobClient.DownloadContentAsync();
        return download.Value.Content.ToString();
    }
    return "";
}



}

