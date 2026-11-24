# 🏋️ FitCore (Gym Management OS)

> A modern, comprehensive Gym Management System for Windows built with .NET 10 and WPF.

![C#](https://img.shields.io/badge/C%23-99.6%25-239120?logo=c-sharp&logoColor=white)
![.NET 10](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet&logoColor=white)
![WPF](https://img.shields.io/badge/WPF-Desktop-0078D6?logo=windows&logoColor=white)
![SQLite](https://img.shields.io/badge/SQLite-Database-003B57?logo=sqlite&logoColor=white)
![Version](https://img.shields.io/badge/Version-1.1.2-blue)

## 📖 Overview

**FitCore** is a robust Gym Management OS designed for Windows. It provides gym owners and administrators with a powerful desktop application to manage daily operations. Built with modern C# architecture, FitCore prioritizes performance, offline data reliability (via SQLite), and a clean, responsive user interface using WPF.

This repository includes everything needed for deployment and evaluation, including a dedicated installer, a demo data seeder, and vendor management tools.

## ✨ Key Features & Tools

*   **Comprehensive Gym Management:** Built to handle the core operational needs of a fitness center.
*   **Demo Seeder (`FitCore.DemoSeeder`):** Includes a standalone tool to instantly populate the database with realistic demo data, making it easy to test and showcase the application.
*   **Vendor Tool (`FitCore.VendorTool`):** A dedicated utility for vendor-related management and operations.
*   **Automated Installer:** Ships with a complete installer configuration for easy distribution to Windows machines.
*   **Unit Testing (`FitCore.Tests`):** A dedicated test suite to ensure core business logic remains reliable.
*   **Modern .NET Architecture:** Utilizes Central Package Management (`Directory.Packages.props`) and modern `.slnx` solution formats for clean dependency management.

## 🛠️ Tech Stack

*   **Language:** C# (99.6%)
*   **Framework:** .NET 10
*   **UI Framework:** WPF (Windows Presentation Foundation)
*   **Database:** SQLite
*   **Build Tooling:** PowerShell (`build.ps1`), MSBuild

## 📂 Project Structure

```text
fitcore-gym-os/
├── src/                   # Main WPF Application source code
├── tests/
│   └── FitCore.Tests/     # Unit and integration tests
├── demo/
│   └── FitCore.DemoSeeder # Utility to seed demo data for evaluation
├── tools/
│   └── FitCore.VendorTool # Standalone vendor management utility
├── installer/             # Installer configuration and assets
├── defaults/              # Default application configurations
├── docs/                  # Project documentation space
├── build.ps1              # PowerShell build automation script
├── FitCore.slnx           # Modern .NET solution file
└── Directory.Packages.props # Central Package Management
