using Azure.Messaging.ServiceBus;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using TGP.AdminPortal.Services;
using TGP.Data;
using TGP.Data.Entities;
using TGP.Data.Messages;

namespace TGP.AdminPortal.Pages.Updates;

[Authorize(Policy = "SystemAdminPolicy")]
public class IndexModel : PageModel
{
    private readonly TgpDbContext _dbContext;
    private readonly ServiceBusClient? _serviceBusClient;
    private readonly IBlobDeletionService? _blobDeletionService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(
        TgpDbContext dbContext,
        IConfiguration configuration,
        ILogger<IndexModel> logger,
        ServiceBusClient? serviceBusClient = null,
        IBlobDeletionService? blobDeletionService = null)
    {
        _dbContext = dbContext;
        _configuration = configuration;
        _logger = logger;
        _serviceBusClient = serviceBusClient;
        _blobDeletionService = blobDeletionService;
    }

    public List<ClientSoftwareVersion> Versions { get; set; } = new();

    [BindProperty]
    public Guid ReleaseId { get; set; }

    public async Task OnGetAsync()
    {
        Versions = await _dbContext.ClientSoftwareVersions
            .OrderByDescending(v => v.ReleaseDate)
            .ToListAsync();
    }

    public async Task<IActionResult> OnPostReleaseAsync()
    {
        var version = await _dbContext.ClientSoftwareVersions.FindAsync(ReleaseId);
        if (version == null) return NotFound();

        version.IsReleased = true;
        await _dbContext.SaveChangesAsync();

        await PublishUpdateMessage(version);

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostUnreleaseAsync()
    {
        var version = await _dbContext.ClientSoftwareVersions.FindAsync(ReleaseId);
        if (version == null) return NotFound();

        version.IsReleased = false;
        await _dbContext.SaveChangesAsync();

        await PublishUpdateMessage(version);

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync()
    {
        var version = await _dbContext.ClientSoftwareVersions.FindAsync(ReleaseId);
        if (version == null) return NotFound();

        // Delete blobs from storage first
        if (_blobDeletionService != null)
        {
            if (!string.IsNullOrEmpty(version.DownloadUrl))
            {
                await _blobDeletionService.DeleteBlobAsync(version.DownloadUrl);
            }
            if (!string.IsNullOrEmpty(version.InstallerUrl))
            {
                await _blobDeletionService.DeleteBlobAsync(version.InstallerUrl);
            }
        }
        else
        {
            _logger.LogWarning("Blob deletion service not configured. Blobs will not be deleted for version {Version}.", version.Version);
        }

        // Remove database record
        _dbContext.ClientSoftwareVersions.Remove(version);
        await _dbContext.SaveChangesAsync();

        // Invalidate cache
        await PublishUpdateMessage(version);

        _logger.LogInformation("Deleted version {Version} for {Platform} (including blobs)", version.Version, version.Platform);

        return RedirectToPage();
    }

    private async Task PublishUpdateMessage(ClientSoftwareVersion version)
    {
        if (_serviceBusClient == null)
        {
            _logger.LogWarning("Service Bus Client is not configured. Cannot publish update notification.");
            return;
        }

        var topicName = _configuration["ServiceBus:ClientUpdatesTopicName"];
        if (string.IsNullOrEmpty(topicName))
        {
            _logger.LogWarning("client updates topic name is not configured.");
            return;
        }

        var sender = _serviceBusClient.CreateSender(topicName);
        var message = new ClientVersionsUpdatedMessage
        {
            Platform = version.Platform,
            Version = version.Version,
            TimestampUtc = DateTime.UtcNow
        };

        var json = JsonSerializer.Serialize(message);
        var busMessage = new ServiceBusMessage(json);

        await sender.SendMessageAsync(busMessage);
    }
}
