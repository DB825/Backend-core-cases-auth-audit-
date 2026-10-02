using Microsoft.Extensions.Options;

namespace CaseAuth.Api.Storage;

// Fixture-mode storage: writes under {LocalDiskRoot}/{firmId}/{caseId}/{guid}_{fileName}.
// Fine for a local/single-instance demo; not a substitute for S3's durability or access
// control, and the fixture root is plain disk, not encrypted at rest.
public class LocalDiskFileStorageService(IOptions<StorageOptions> options) : IFileStorageService
{
    public async Task<string> SaveAsync(string firmId, Guid caseId, string fileName, Stream content, CancellationToken ct)
    {
        var safeFileName = Path.GetFileName(fileName);
        var relativeKey = Path.Combine(firmId, caseId.ToString(), $"{Guid.NewGuid()}_{safeFileName}");
        var fullPath = Path.Combine(options.Value.LocalDiskRoot, relativeKey);

        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

        await using var fileStream = File.Create(fullPath);
        await content.CopyToAsync(fileStream, ct);

        return relativeKey.Replace(Path.DirectorySeparatorChar, '/');
    }
}
