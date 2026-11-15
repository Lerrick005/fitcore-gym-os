# FitCore - Assumptions

This file records every working assumption made while building FitCore, per PRS §0 rule 2 and §25.
When an assumption changes, update this file and note the date.

## Product-owner decisions (PRS §25)

| # | Decision | Working assumption | Status |
|---|---|---|---|
| D1 | One workstation per gym in v1 | Yes - single-gym, single-workstation, no LAN, no cloud | CONFIRM |
| D2 | eTIMS / fiscal receipts needed in v1 | No - extension point `IFiscalReceiptProvider` only | CONFIRM |
| D3 | Installer technology | Inno Setup (simpler than WiX for a vendor-run desktop install) | CONFIRM |
| D4 | PDF library | QuestPDF if community licence terms fit; else FlowDocument/XPS | CONFIRM |
| D5 | Webcam library | OpenCvSharp4 behind `IPhotoCaptureService`; file upload always available | CONFIRM |
| D6 | Thermal printing method | Windows print driver first; ESC/POS optional | CONFIRM |

## Build-time decisions (recorded during build)

| Date | Area | Decision | Reason |
|---|---|---|---|
| 2026-11-13 | Runtime | Target `net10.0` | Installed SDK is 10.0.402; .NET 10 is current LTS |
| 2026-11-13 | Repo root | `C:\Users\Administrator\GYM\FitCore` | Keeps solution clean under the terminal working dir |
| 2026-11-13 | Package versions | Pinned in `Directory.Packages.props` (Step 2) | §3 requires verification and pinning at build time |

## Open questions / deferred

- CsvHelper and test packages (xUnit, Shouldly, NSubstitute, Bogus) versions: to be pinned in Step 2 after a restore check.
- LiveChartsCore.SkiaSharpView.WPF version: verify in Step 2 (may be newer than 2.0.1).

## Step 3 - Schema build decisions (2026-11-13)

| Area | Decision | Reason |
|---|---|---|
| Solution format | `.slnx` (XML) | .NET 10 default; supported by VS 2022 17.13+ and `dotnet` CLI |
| SQLite CVE pin | `SQLitePCLRaw.bundle_e_sqlite3 2.1.12` | Fixes NU1903 (CVE-2025-6965 in 2.1.11); pinned in Directory.Packages.props |
| Serilog version | 4.3.0 | Required by Serilog.Extensions.Hosting 10.0.0 |
| EF Core SQLite DDL style | FKs inline in `CreateTable(...).constraints(...)`, not `AddForeignKey` calls | EF Core 10 SQLite provider behaviour; not a bug |
| Navigation properties | None in v1 entities | FK ints only; nav props added in Step 8 when services load graphs |
| Primary keys | `int` autoincrement for business tables; `Guid` only for `Installation.Id`; `long` for `AuditLog.Id` | Faster index/join on SQLite; no multi-tenancy in v1 |
| Timestamps | `DateTime` UTC, EF SQLite default mapping to ISO-8601 `TEXT` | Per PRS §13 ("ISO-8601 text") |
| Money in entities | `long AmountMinor` + `string CurrencyCode` columns directly (not `Money` value object at DB boundary) | Matches §13 column-by-column; `Money` value object used in domain arithmetic (Step 8) |
| `RolePermission` | Composite PK `(RoleId, PermissionKey)`; no surrogate Id | Natural key |
| `ModuleState` class | Maps to table `Modules` | Avoids `System.Reflection.Module` collision; table name matches §13.1 |
| `Referral` timestamp | `ReferredAtUtc` (renamed from `CreatedAt`) | Distinguishes "when referral happened" from `AuditableEntity.CreatedAtUtc` (row creation) |
| Append-only enforcement | 4 SQLite triggers in `InitialSchema.Up()` | §13.5 "where practical, SQLite triggers" |
| `__EFMigrationsHistory` | Internal EF table, not part of PRS schema | Verified 33 PRS tables + 1 internal table |
| Test packages | xUnit 2.9.3, xunit.runner.visualstudio 3.1.1, Shouldly 4.3.0, NSubstitute 5.3.0, Bogus 35.6.1 | All pinned, all permissive licences |

## Step 4 - Settings Service + Module Registry decisions (2026-11-13)

| Area | Decision | Reason |
|---|---|---|
| `ISettingsGroup` | Marker with `static abstract string GroupKey` | Compile-time safe; catches missing key at build time |
| Settings storage | One `SettingsEntries` row per group, JSON via `System.Text.Json`, camelCase **off** | §6 "typed, validated groups"; deterministic serialisation |
| Settings cache | In-process `ConcurrentDictionary<string, object?>` | Single process, single workstation (§5) — no external cache package |
| Save concurrency | Per-group `SemaphoreSlim`; cross-group saves independent | Prevents lost updates on same group without blocking unrelated groups |
| Save transactionality | One `BeginTransaction` covering settings upsert + audit write | §4.2 "runs in a unit of work/transaction" |
| No-op save | Skipped (no transaction, no audit) | Avoids audit noise; only real changes are logged |
| `Changed` event timing | Raised **after** commit | Subscribers see persisted state |
| Permission model | `settings.manage` required for `SaveAsync`; bypass only inside `SettingsSystemContext.Enter()` | §10 wizard runs before auth exists |
| `SettingsSystemContext` | `AsyncLocal<bool>` scope; cannot leak beyond `using` block | Task- and thread-safe; scope-limited |
| `ICurrentUser` | `StubCurrentUser` for Steps 4-5; `HasPermission` returns `SettingsSystemContext.IsActive` | Real auth replaces in Step 6 |
| `ISettingsService` lifetime | Scoped (holds scoped `DbContext`) | WPF shell holds reference; tests confirm |
| `IModuleRegistry` lifetime | Singleton (stateless catalogue) | Fail-fast validation at construction |
| `IModuleAccessGuard` lifetime | Scoped (needs `DbContext`) | Reads only; mutating module state is Step 11 |
| `ModuleAccessGuard` unknown key | Returns `false` / `Result.Fail("module.unknown", ...)` — fail-closed | §9.3 point 2 ("including direct navigation") |
| Module dependency graph | Validated at `ModuleRegistry` construction: unique keys, all deps exist, no cycles | Fail-fast; bad catalogue cannot run |
| Test permission pattern | `FakeCurrentUser` for tests that need permission; real `StubCurrentUser` for the denial test and the wizard-bypass test | Tests exercise both paths honestly |

## .NET 10 async LINQ ambiguity rule (2026-11-13)

In .NET 10, `DbSet<T>` implements both `IQueryable<T>` (EF Core extensions)
and `IAsyncEnumerable<T>` (built-in `System.Linq.AsyncEnumerable` extensions).
Calls like `.CountAsync()`, `.SingleAsync()`, `.AnyAsync()`, `.FirstAsync()`
on a `DbSet<T>` are ambiguous and fail to compile with CS0411.

**Resolution:** call the EF Core extension statically:
- `EntityFrameworkQueryableExtensions.CountAsync(db.Set)`
- `EntityFrameworkQueryableExtensions.SingleAsync(db.Set, predicate)`

**Do NOT** rely on `System.Linq.Async` package + `ExcludeAssets=compile`.
It does not resolve the ambiguity when the package is transitive — we tested
this and it still failed.

**Future helper (Step 6+):** a `DbSetQueryExtensions` with distinct method
names (`CountEfAsync`, `SingleEfAsync`) will wrap the static calls to keep
call sites readable.

## Step 6 - Authentication, permissions, recovery (2026-11-13)

| Area | Decision | Reason |
|---|---|---|
| Session storage | In-memory only (no "remember me") | §11 + §5 (single workstation, shared reception terminal) |
| Lockout policy | 5 failed attempts → 15 min lockout; failed attempt #5 still reports "invalid", attempt #6 reports "locked" | Standard rate-limit UX; prevents username enumeration on the exact lock trigger |
| `CurrentUser.HasPermission` | Lazy DB query per call (no role cache) | Simplicity; performance impact negligible at reception-scale usage; can optimize later |
| Auto-login after wizard | Yes — `CreateFirstOwnerAsync` then `LoginAsync` in `RoleAndOwnerViewModel.SaveAsyncCore` | Smoother onboarding; user doesn't immediately have to log in |
| First Owner creation | `IUserCreationService.CreateFirstOwnerAsync` only path to create Owner; cannot be created via `CreateUserAsync` | Prevents accidental multiple-Owner creation |
| Last-Owner protection | Enforced in `ChangeRoleAsync` and `SetActiveAsync`; error code `identity.owner.last` | §11 "at least one active Owner must always exist" |
| Role seeding | Idempotent — creates missing permissions and default roles; does NOT overwrite existing roles | §11 renamable/editable roles; owner edits preserved across upgrades |
| Recovery challenge format | `CHAL-{shortInst}-{shortId}-{expiry}`; recovery code `{challengeId}\|{sigBase64}\|{expiryUnix}` | Human-readable challenge; unambiguous machine-parseable recovery code |
| Recovery signature | RSA-PSS SHA-256, 2048-bit key, signed by vendor `FitCore.VendorTool`, verified in-app with embedded public key | §11 + §14 (asymmetric signing, private key never ships) |
| Recovery single-use | `RecoveryAttempt.UsedAtUtc` set BEFORE password reset | Prevent replay |
| Recovery deactivated-user check | Refused with `user.inactive` | §11 intent (deactivated accounts can't be recovered to active) |
| Recovery code entry UI | 3-step `ForgotPasswordWindow` (challenge → paste code → new password) | Matches PRS §11 flow, plain language, no jargon |
| Vendor key location | `tools/FitCore.VendorTool/Keys/vendor-private.pem` (gitignored) + `src/FitCore.Desktop/Resources/vendor-public-key.txt` | Public key ships; private key never leaves vendor |
| Test signing key | Test reads private key from `tools/FitCore.VendorTool/Keys/vendor-private.pem` for signing recovery codes | Tests sign inline (no shelling to exe); production vendor uses CLI |
| Schema | `RecoveryAttempts` is table 34; `SchemaTests` updated to assert 34 | Legitimate extension beyond PRS §13 list |
