# FitCore - Architecture

> Skeleton created in Step 1. Filled in during Step 2 (solution structure, DI host, logging, single-instance guard)
> and expanded in later steps per PRS §20.

## Layers (PRS §4.2)
- **FitCore.Domain** - entities, value objects, enums, domain rules. No dependencies.
- **FitCore.Application** - use cases, Settings Service, Module Registry, permission checks, DTOs, validators, ports.
- **FitCore.Infrastructure** - EF Core/SQLite, migrations, repositories, file storage, backup, licence verifier, hashing, printing, import/export adapters.
- **FitCore.Desktop** - WPF shell, theming, navigation, wizard, views, view models.
- **FitCore.VendorTool** - vendor-only, never shipped.

Dependencies point inward only: Desktop -> Application -> Domain; Infrastructure implements Application interfaces.

## Cross-cutting services (interfaces in Application)
`ISettingsService`, `IModuleRegistry`, `IModuleAccessGuard`, `IPermissionService`, `ICurrentUser`, `IAuditLogger`,
`IClock`, `ILicenceService`, `IBackupService`, `IReceiptPrinter`, `IPhotoCaptureService`, `ILocalizer`,
`IFileStorage`, `IImportService`, `IExportService`.

## Status
- Step 1: skeleton only.
- Step 2: solution/project structure, DI host, logging, single-instance guard, empty tests.
