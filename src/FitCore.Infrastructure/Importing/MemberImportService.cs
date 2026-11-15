using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FitCore.Application.Abstractions;
using FitCore.Application.Common;
using FitCore.Application.Importing;
using FitCore.Application.Members;
using FitCore.Domain.Enums;
using FitCore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FitCore.Infrastructure.Importing;

public sealed class MemberImportService : IMemberImportService
{
    private readonly FitCoreDbContext _db;
    private readonly IMemberService _members;
    private readonly ICurrentUser _user;
    private readonly IClock _clock;
    private readonly IAuditLogger _audit;

    public MemberImportService(FitCoreDbContext db, IMemberService members, ICurrentUser user, IClock clock, IAuditLogger audit)
    {
        _db = db;
        _members = members;
        _user = user;
        _clock = clock;
        _audit = audit;
    }

    public async Task<Result<MemberImportPreview>> PreviewAsync(string csvFilePath, CancellationToken ct = default)
    {
        return await ProcessAsync(csvFilePath, dryRun: true, ct);
    }

    public async Task<Result<MemberImportPreview>> CommitAsync(string csvFilePath, CancellationToken ct = default)
    {
        return await ProcessAsync(csvFilePath, dryRun: false, ct);
    }

    private async Task<Result<MemberImportPreview>> ProcessAsync(string csvFilePath, bool dryRun, CancellationToken ct)
    {
        if (!_user.HasPermission("members.import"))
            return Result<MemberImportPreview>.Fail("permission.denied", "You do not have permission to import members.");

        if (!File.Exists(csvFilePath))
            return Result<MemberImportPreview>.Fail("import.file_not_found", "File not found.");

        // Parse CSV
        List<MemberImportRow> rows;
        try
        {
            rows = ParseCsv(csvFilePath);
        }
        catch (Exception ex)
        {
            return Result<MemberImportPreview>.Fail("import.parse_failed", $"Could not parse CSV: {ex.Message}");
        }

        if (rows.Count == 0)
            return Result<MemberImportPreview>.Fail("import.empty", "No data rows found.");

        // Get existing phones for duplicate detection
        var existingPhones = await _db.Members
            .AsQueryable()
            .Where(m => m.ArchivedAtUtc == null)
            .Select(m => m.Phone)
            .ToListAsync(ct);
        var phoneSet = new HashSet<string>(existingPhones, StringComparer.OrdinalIgnoreCase);

        var results = new List<MemberImportRowResult>();
        var toCreate = new List<MemberImportRow>();

        foreach (var row in rows)
        {
            // Validate required fields
            if (string.IsNullOrWhiteSpace(row.FullName) || row.FullName.Length > 200)
            {
                results.Add(new MemberImportRowResult(row.RowNumber, row.FullName, row.Phone, "Invalid", "Full name missing or too long."));
                continue;
            }
            if (string.IsNullOrWhiteSpace(row.Phone) || row.Phone.Length > 30)
            {
                results.Add(new MemberImportRowResult(row.RowNumber, row.FullName, row.Phone, "Invalid", "Phone missing or too long."));
                continue;
            }
            if (!string.IsNullOrWhiteSpace(row.Email) && !row.Email.Contains('@'))
            {
                results.Add(new MemberImportRowResult(row.RowNumber, row.FullName, row.Phone, "Invalid", "Email looks invalid."));
                continue;
            }

            // Duplicate check
            if (phoneSet.Contains(row.Phone.Trim()))
            {
                results.Add(new MemberImportRowResult(row.RowNumber, row.FullName, row.Phone, "Duplicate", "Phone already exists in the system."));
                continue;
            }

            // Valid — add to create list and mark phone in set (prevent intra-file dups)
            results.Add(new MemberImportRowResult(row.RowNumber, row.FullName, row.Phone, "Ready", null));
            toCreate.Add(row);
            phoneSet.Add(row.Phone.Trim());
        }

        var preview = new MemberImportPreview(
            TotalRows: rows.Count,
            ReadyCount: results.Count(r => r.Status == "Ready"),
            DuplicateCount: results.Count(r => r.Status == "Duplicate"),
            InvalidCount: results.Count(r => r.Status == "Invalid"),
            RowResults: results);

        if (dryRun) return Result<MemberImportPreview>.Ok(preview);

        // COMMIT — create members
        var created = 0;
        var failed = 0;
        var updatedResults = new List<MemberImportRowResult>(results);

        foreach (var row in toCreate)
        {
            try
            {
                var input = new MemberInput(
                    row.FullName.Trim(),
                    row.Phone.Trim(),
                    string.IsNullOrWhiteSpace(row.Email) ? null : row.Email.Trim(),
                    ParseDate(row.DateOfBirth),
                    string.IsNullOrWhiteSpace(row.Gender) ? null : row.Gender.Trim(),
                    null,
                    string.IsNullOrWhiteSpace(row.Notes) ? null : row.Notes.Trim());

                var r = await _members.CreateAsync(input, ct);
                if (r.IsSuccess) { created++; }
                else
                {
                    failed++;
                    var idx = updatedResults.FindIndex(x => x.RowNumber == row.RowNumber);
                    if (idx >= 0)
                        updatedResults[idx] = updatedResults[idx] with { Status = "Error", Message = r.ErrorMessage };
                }
            }
            catch (Exception ex)
            {
                failed++;
                var idx = updatedResults.FindIndex(x => x.RowNumber == row.RowNumber);
                if (idx >= 0)
                    updatedResults[idx] = updatedResults[idx] with { Status = "Error", Message = ex.Message };
            }
        }

        await _audit.LogAsync("members.import", "Member", null, null,
            $"created={created} failed={failed} total={rows.Count}", null, ct);

        var final = preview with
        {
            ReadyCount = created,
            RowResults = updatedResults
        };
        return Result<MemberImportPreview>.Ok(final);
    }

    private static DateTime? ParseDate(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;
        if (DateTime.TryParseExact(text.Trim(), new[] { "yyyy-MM-dd", "dd/MM/yyyy", "MM/dd/yyyy", "d/M/yyyy" },
                CultureInfo.InvariantCulture, DateTimeStyles.None, out var d))
            return d;
        return null;
    }

    private static List<MemberImportRow> ParseCsv(string path)
    {
        var lines = File.ReadAllLines(path);
        if (lines.Length < 2)
            throw new InvalidOperationException("CSV must have a header row and at least one data row.");

        // Parse header — find column indices
        var header = SplitCsvLine(lines[0]).Select((h, i) => new { Name = h.Trim().ToLowerInvariant(), Index = i })
            .ToDictionary(x => x.Name, x => x.Index);

        int FindCol(params string[] names)
        {
            foreach (var n in names)
                if (header.TryGetValue(n, out var idx)) return idx;
            return -1;
        }

        var nameCol = FindCol("fullname", "full name", "name");
        var phoneCol = FindCol("phone", "phone number", "phonenumber", "mobile");
        var emailCol = FindCol("email", "email address");
        var dobCol = FindCol("dateofbirth", "date of birth", "dob", "birthdate");
        var genderCol = FindCol("gender", "sex");
        var notesCol = FindCol("notes", "comment", "comments");

        if (nameCol < 0 || phoneCol < 0)
            throw new InvalidOperationException("CSV must include 'FullName' and 'Phone' columns (case-insensitive).");

        var rows = new List<MemberImportRow>();
        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;
            var cells = SplitCsvLine(lines[i]);

            string Get(int col) => col >= 0 && col < cells.Count ? cells[col].Trim() : "";

            rows.Add(new MemberImportRow(
                RowNumber: i + 1,
                FullName: Get(nameCol),
                Phone: Get(phoneCol),
                Email: emailCol >= 0 ? Get(emailCol) : null,
                DateOfBirth: dobCol >= 0 ? Get(dobCol) : null,
                Gender: genderCol >= 0 ? Get(genderCol) : null,
                Notes: notesCol >= 0 ? Get(notesCol) : null));
        }

        return rows;
    }

    /// <summary>Minimal CSV line splitter — handles quoted fields with commas.</summary>
    private static List<string> SplitCsvLine(string line)
    {
        var cells = new List<string>();
        var cur = new System.Text.StringBuilder();
        var inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (c == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"') { cur.Append('"'); i++; }
                else inQuotes = !inQuotes;
            }
            else if (c == ',' && !inQuotes) { cells.Add(cur.ToString()); cur.Clear(); }
            else cur.Append(c);
        }
        cells.Add(cur.ToString());
        return cells;
    }
}
