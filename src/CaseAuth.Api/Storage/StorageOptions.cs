namespace CaseAuth.Api.Storage;

public class StorageOptions
{
    public const string SectionName = "Storage";

    public string Mode { get; set; } = "LocalDisk";
    public string LocalDiskRoot { get; set; } = "fixture-uploads";
    public long MaxUploadBytes { get; set; } = 15 * 1024 * 1024;

    // Left empty by default (rather than pre-populated) because the .NET configuration binder
    // appends to, rather than replaces, a non-empty array default - set actual values in
    // appsettings.json's Storage:AllowedContentTypes.
    public string[] AllowedContentTypes { get; set; } = [];
}
