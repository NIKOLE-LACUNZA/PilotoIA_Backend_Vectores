using Azure.Storage.Blobs;
public class AzureBlobService
{
    private readonly BlobContainerClient _container;

    public AzureBlobService(IConfiguration config)
    {
        var connectionString = config["AzureBlobStorage:ConnectionString"];
        var containerName = config["AzureBlobStorage:ContainerName"];
        _container = new BlobContainerClient(connectionString, containerName);
        _container.CreateIfNotExists();
    }

    public async Task<string> UploadFileAsync(IFormFile file)
    {
        var blobClient = _container.GetBlobClient(file.FileName);
        using var stream = file.OpenReadStream();
        await blobClient.UploadAsync(stream, true);
        return blobClient.Uri.ToString();
    }
}
