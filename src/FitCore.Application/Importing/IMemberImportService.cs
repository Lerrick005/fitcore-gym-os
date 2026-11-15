using FitCore.Application.Common;

namespace FitCore.Application.Importing;

public interface IMemberImportService
{
    /// <summary>Parse a CSV file and dry-run validate. Does not write anything.</summary>
    Task<Result<MemberImportPreview>> PreviewAsync(string csvFilePath, CancellationToken ct = default);

    /// <summary>Commit the valid rows from a previously previewed file. Skips duplicates and invalids.</summary>
    Task<Result<MemberImportPreview>> CommitAsync(string csvFilePath, CancellationToken ct = default);
}
