using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace TGP.AdminPortal.Services;

public interface IBlobDeletionService
{
    Task<bool> DeleteBlobAsync(string sasUrl, CancellationToken ct = default);
}

public class BlobDeletionService : IBlobDeletionService
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly ILogger<BlobDeletionService> _logger;

    public BlobDeletionService(
        BlobServiceClient blobServiceClient,
        ILogger<BlobDeletionService> logger)
    {
        _blobServiceClient = blobServiceClient;
        _logger = logger;
    }

    public async Task<bool> DeleteBlobAsync(string sasUrl, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(sasUrl))
        {
            _logger.LogWarning("DeleteBlobAsync called with empty URL, skipping");
            return false;
        }

        try
        {
            // Extract container and blob name from the SAS URL
            // URL format: https://<account>.blob.core.windows.net/<container>/<blobname>?<sas-token>
            var uri = new Uri(sasUrl);
            var pathSegments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
            
            if (pathSegments.Length < 2)
            {
                _logger.LogWarning("Could not extract blob name from URL: {Url}", sasUrl);
                return false;
            }

            var containerName = pathSegments[0];
            var blobName = string.Join("/", pathSegments.Skip(1));
            
            _logger.LogInformation("Deleting blob {BlobName} from container {ContainerName} (including snapshots)", 
                blobName, containerName);

            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            var blobClient = containerClient.GetBlobClient(blobName);
            
            var response = await blobClient.DeleteIfExistsAsync(
                DeleteSnapshotsOption.IncludeSnapshots, 
                cancellationToken: ct);

            if (response.Value)
            {
                _logger.LogInformation("Successfully deleted blob {BlobName}", blobName);
            }
            else
            {
                _logger.LogInformation("Blob {BlobName} did not exist", blobName);
            }

            return response.Value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete blob from URL: {Url}", sasUrl);
            // Don't throw - deletion failure shouldn't block database cleanup
            return false;
        }
    }
}
