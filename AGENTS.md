# dotnet-up — Agent Notes

## Project overview

`dotnet-up` is a .NET global tool (`dotnet up`) that reads a `dotnet-up.json` config file and launches all defined processes in parallel, each in its own terminal window.

- Source: `src/` (.NET 10, `PackAsTool`)
- Schema: `schema/dotnet-up.schema.json`
- Packed output: `nupkg/` (excluded from git)

## Build & pack

```bash
# Build
dotnet build src/dotnet-up.csproj

# Pack NuGet
dotnet pack src/dotnet-up.csproj -c Release

# Install locally for testing
dotnet tool install -g --add-source ./nupkg dotnet-up
dotnet tool update  -g --add-source ./nupkg dotnet-up
```

## Project structure

```
src/
  Commands/   # CLI command handlers (up, init, ...)
  Config/     # dotnet-up.json deserialization
  Runner/     # Process launching logic
  Update/     # Self-update / version check logic
  Program.cs  # Entry point
schema/
  dotnet-up.schema.json
README.md
```

## Versioning

Version is set in `src/dotnet-up.csproj` → `<Version>`. Bump this before packing a new release.

## Publishing

Publishing to NuGet.org is automated via `.github/workflows/publish.yml`.

**To release:**
1. Bump `<Version>` in `src/dotnet-up.csproj`
2. Commit and push
3. Create a GitHub Release — the workflow triggers automatically on `published`

The workflow packs in Release mode and pushes to NuGet.org using the `NUGET_API_KEY` repository secret. Add this secret at:
`GitHub repo → Settings → Secrets and variables → Actions → NUGET_API_KEY`

## GitHub files

| File | Purpose |
|------|---------|
| `LICENSE` | MIT license (Søren Kottal) |
| `.github/FUNDING.yml` | GitHub Sponsors button (`github: skttl`) |
| `.github/workflows/publish.yml` | Auto-publish to NuGet on release |

## Notes

- Target framework: `net10.0`
- `nupkg/` is gitignored — build locally to produce packages
- `dotnet-up.json` files are gitignored to avoid accidentally committing local dev configs
