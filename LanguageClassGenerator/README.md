# LanguageClassGenerator

A .NET console tool that generates `LangProvider` and
`LangKeys` classes from a `.resx` resource file. These
generated classes provide strongly-typed, data-bindable
access to localized strings in the FoliCon WPF application.

## What It Does

Reads `Properties/Langs/Lang.resx`, extracts all string
resource keys, and generates `LangProvider1.cs` containing:

- **`LangProvider`** — An `INotifyPropertyChanged` class
  with a property per resource key, enabling WPF data
  binding and runtime language switching via `UpdateLangs()`.
- **`LangKeys`** — A companion class with `static string`
  fields for each key, useful for programmatic lookups.

## Usage

```bash
# Default — reads Properties/Langs/Lang.resx, writes LangProvider1.cs
dotnet run --project LanguageClassGenerator

# Custom paths
dotnet run --project LanguageClassGenerator -- <path-to-resx> <output-path>
```

## Requirements

- .NET 9.0 SDK

## Project Structure

```text
LanguageClassGenerator/
├── Program.cs                    # Generator logic
├── LanguageClassGenerator.csproj
├── Properties/Langs/Lang.resx    # Source resource file
├── LangProvider1.cs              # Generated output (not compiled)
└── README.md
```

## How It Fits in FoliCon

`LangProvider1.cs` is an output artifact — it is **not**
compiled by this project. After generation, it is used by
the [FoliCon](../FoliCon/) WPF application alongside the
existing [`FoliCon/Modules/LangProvider.cs`][langprovider].

[langprovider]: ../FoliCon/Modules/LangProvider.cs
