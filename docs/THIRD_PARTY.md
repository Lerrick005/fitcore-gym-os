# FitCore - Third-Party Dependencies

Record every third-party library, its licence, and why it is used (PRS §3).
All libraries below must be permissively licensed. No preview packages.

| Package | Version (pinned) | Licence | Purpose |
|---|---|---|---|
| CommunityToolkit.Mvvm | 8.4.2 | MIT | MVVM source generators (ObservableProperty, RelayCommand) |
| Microsoft.EntityFrameworkCore.Sqlite | 10.0.10 | MIT | Persistence |
| Microsoft.EntityFrameworkCore.Design | 10.0.10 | MIT | Migrations tooling |
| Microsoft.Extensions.Hosting | 10.0.10 | MIT | DI host, options |
| Microsoft.Extensions.DependencyInjection | 10.0.10 | MIT | DI |
| Microsoft.Extensions.Options | 10.0.10 | MIT | Typed options |
| Serilog | 4.x | Apache-2.0 | Structured logging |
| Serilog.Extensions.Hosting | 10.0.0 | Apache-2.0 | Serilog + generic host |
| Serilog.Sinks.File | 7.0.0 | Apache-2.0 | Rolling file sink |
| FluentValidation | 12.2.0 | Apache-2.0 | Service-layer validation |
| Konscious.Security.Cryptography.Argon2 | 1.3.1 | MIT | Argon2id password hashing |
| QuestPDF | 2026.9.1 | QuestPDF Community (check terms) | PDF generation |
| ClosedXML | 0.105.1 | MIT | XLSX import/export |
| CsvHelper | TBD (Step 2) | Apache-2.0 / MS-PL | CSV import/export |
| LiveChartsCore.SkiaSharpView.WPF | TBD (Step 2) | MIT | Charts |
| ZXing.Net | 0.16.11 | Apache-2.0 | QR/barcode generation |
| OpenCvSharp4 | TBD (Step 2) | Apache-2.0 | Webcam capture behind IPhotoCaptureService |
| xUnit | TBD (Step 2) | Apache-2.0 | Unit/integration tests |
| Shouldly | TBD (Step 2) | BSD-3-Clause | Assertions |
| NSubstitute | TBD (Step 2) | BSD-3-Clause | Mocking |
| Bogus | TBD (Step 2) | MIT | Test data + demo seeder |

## Rules
- Every library must be permissively licensed and recorded here before use.
- No preview packages.
- Check licence terms at every version bump.
- QuestPDF: verify community licence eligibility before shipping.
