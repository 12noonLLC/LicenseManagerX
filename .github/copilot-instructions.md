# License Manager X - Copilot Instructions

## Project Overview

License Manager X is a .NET-based license management application for creating and validating software licenses. It consists of:
- **LicenseManagerX**: WPF desktop application (main UI)
- **LicenseManagerX.Console**: Command-line interface for license generation
- **LicenseManager_12noon.Client**: NuGet package for license validation in client applications
- **LicenseManagerX_Example**: Example application demonstrating license validation
- **LicenseManagerX.UnitTests**: Unit test suite

The project uses the Standard.Licensing library for cryptographic license generation and validation.

**Platform Requirements**: This project targets Windows (win-x64) due to WPF and Windows-specific components.
Build and test commands require a Windows environment.
However, prefer keeping the LicenseManager_12noon.Client package platform-neutral unless
a Windows-specific target is technically required by the build or publish graph.

## Build and Test Commands

### Build
```bash
# Clean and restore
dotnet clean --configuration Release --runtime win-x64
dotnet nuget locals all --clear

# Build main application (requires the matching .NET SDK installed)
dotnet build LicenseManagerX/LicenseManagerX.csproj --configuration Release --runtime win-x64

# Build NuGet client library (multi-target: .NET 8, .NET 9, and .NET 10)
dotnet build LicenseManager_12noon.Client/LicenseManager_12noon.Client.csproj --configuration Release --runtime win-x64 --framework net8.0
dotnet build LicenseManager_12noon.Client/LicenseManager_12noon.Client.csproj --configuration Release --runtime win-x64 --framework net9.0
dotnet build LicenseManager_12noon.Client/LicenseManager_12noon.Client.csproj --configuration Release --runtime win-x64 --framework net10.0

# Build entire solution
dotnet build LicenseManagerX.slnx --configuration Release --runtime win-x64
```

### Test
```bash
# Run unit tests (both Release and Debug configurations)
dotnet test --project LicenseManagerX.UnitTests/LicenseManagerX.UnitTests.csproj --no-ansi --no-progress --output Detailed --runtime win-x64 --configuration Release
dotnet test --project LicenseManagerX.UnitTests/LicenseManagerX.UnitTests.csproj --no-ansi --no-progress --output Detailed --runtime win-x64 --configuration Debug
```

### Package
```bash
# Create NuGet package
dotnet pack LicenseManager_12noon.Client/LicenseManager_12noon.Client.csproj --configuration Release --runtime win-x64 --no-restore --no-build --output ./release/
```

## Code Style and Conventions

### General Guidelines
- Maintain existing code structure and organization.
- Write unit tests for new functionality. Use table-driven unit tests when possible.
- Document complex logic. Suggest changes to the `README.md` when appropriate.
- Do not delete existing comments.

### C# Code Style
- Prefer to use explicit type instead of var. Example: `string s = new();`
- Prefer to assign `new()` or `[]` where possible.
- Use tabs instead of spaces, even in XAML. A tab is equivalent to three spaces.
- Use braces around single-line expressions.
- Use parentheses around binary conditional expressions. Example: `if ((x > 0) && (y > 0))` but `if (x && y)`
- Add a comma after the last item in an initializer list.

### Project Configuration
- Target frameworks: .NET 10 (main app), .NET 8/.NET 9/.NET 10 (NuGet client)
- Platform: x64 (Windows)
- Nullable reference types: Enabled
- Implicit usings: Disabled (use explicit using statements)

## Git and Version Control

- Do not add build artifacts to git. Ignore directories such as `bin/` and `obj/`.
- Version information is centralized in `Directory.Build.props`.
- Follow semantic versioning for releases.

## Testing Guidelines

- Use MSTest framework for unit tests.
- Place tests in the `LicenseManagerX.UnitTests` project.
- Use table-driven tests where multiple test cases validate similar behavior.
- Test both success and failure scenarios.
- Validate edge cases (empty values, special characters, null handling).

## Architecture Notes

- The application uses CommunityToolkit.Mvvm for MVVM pattern in the WPF application.
- License files use XML format with digital signatures.
- Public/private key pairs are generated using a passphrase.
- Product ID and public key are used by client applications to validate licenses.
- `.private` files contain sensitive keypair information and should never be committed to source control.
