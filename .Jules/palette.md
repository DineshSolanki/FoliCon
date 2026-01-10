## 2024-05-23 - Tech Stack Ambiguity in Instructions
**Learning:** The user instructions provided conflicting signals: the prompt text described a "WPF desktop app", but the sample commands (`pnpm`, `tsx`) and code snippets were for a React/Web stack.
**Action:** When instructions conflict with the codebase reality, always trust the file structure (`.sln`, `.csproj` vs `package.json`) and adapt the workflow (e.g., switch from `pnpm test` to `dotnet test` or Visual Studio build verification). Do not blindly run commands that don't match the stack.
