# Contributing to FoliCon

Thanks for your interest in contributing! Here's how to get started.

## Prerequisites

- [.NET 9 SDK](https://aka.ms/dotnet-download) or later
- [Visual Studio 2022](https://visualstudio.microsoft.com/) or [JetBrains Rider](https://www.jetbrains.com/rider/) with WPF/.NET workloads
- Windows 10/11 (WPF project)

## Building

```bash
git clone https://github.com/DineshSolanki/FoliCon.git
cd FoliCon
dotnet build
```

## Project Structure

| Directory | Purpose |
|---|---|
| `FoliCon/` | Main WPF application |
| `FoliCon/Modules/` | Core logic (TMDB, IGDB, utilities) |
| `FoliCon/ViewModels/` | Prism MVVM ViewModels |
| `FoliCon/Views/` | XAML Views |
| `FoliCon/Models/` | Data models |
| `FoliCon/Properties/Langs/` | Localization resources |
| `LanguageClassGenerator/` | Tool for generating language helper classes |

## Making Changes

1. **Fork** the repository
2. **Create a branch** from `master`:
   ```bash
   git checkout -b feature/my-feature
   ```
3. **Make your changes** following the coding conventions below
4. **Build and test** locally:
   ```bash
   dotnet build
   ```
5. **Commit** with a clear message describing what and why
6. **Push** and open a **Pull Request** against `master`

## Coding Conventions

- **Formatting**: Follow the `.editorconfig` in the repo root — it enforces style automatically
- **Braces**: Always use braces, even for single-line blocks
- **Namespaces**: Use file-scoped namespaces (`namespace Foo;`)
- **var**: Use `var` when the type is apparent
- **Naming**:
  - Private fields: `_camelCase`
  - Public members: `PascalCase`
  - Constants: `camelCase`
  - Interfaces: `IPrefix`
- **MVVM**: Follow Prism conventions — logic in ViewModels, Views are XAML-only
- **Logging**: Use NLog with structured placeholders (`Logger.Info("Saving to {Path}", path)`)

## Pull Request Guidelines

- Fill out the PR template
- Reference the issue your PR addresses (e.g., `Closes #123`)
- Keep PRs focused — one feature or fix per PR
- Ensure the CI build (SonarCloud) passes
- Respond to review feedback

## Localization

FoliCon uses Crowdin for translations. To contribute translations:

1. Visit the [FoliCon Crowdin project](https://crowdin.com/project/folicon)
2. Translate strings in your language
3. Translations are automatically synced via CI

To add a new language, open an issue first so it can be added to Crowdin.

## Reporting Bugs

Use the [Bug Report](https://github.com/DineshSolanki/FoliCon/issues/new?template=bug_report.md) template. Include:

- Steps to reproduce
- Expected vs actual behavior
- Screenshots if applicable
- FoliCon version and Windows version

## Feature Requests

Use the [Feature Request](https://github.com/DineshSolanki/FoliCon/issues/new?template=feature_request.md) template.

## License

By contributing, you agree that your contributions will be licensed under the [GPL-3.0 License](LICENSE).
