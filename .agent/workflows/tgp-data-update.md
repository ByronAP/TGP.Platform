---
description: How to update TGP.Data and consuming projects
---

# TGP.Data Update Workflow

## CRITICAL RULES
- **NEVER use ProjectReference** - always use PackageReference with `Version="*-*"`
- **NEVER add submodules** - the monorepo has a fixed submodule structure

## Workflow Steps

// turbo-all

1. Make changes to `TGP.Data` (entities, migrations, etc.)

2. Build and verify locally:
   ```
   dotnet build TGP.Data\src\TGP.Data\TGP.Data.csproj
   ```

3. Commit and push TGP.Data:
   ```
   git add -A && git commit -m "feat: <description>"
   git push origin main
   ```

4. **Wait 60 seconds** for NuGet package to publish

5. In consuming projects, restore packages with no-cache:
   ```
   dotnet restore <project>.csproj --no-cache
   ```

6. Build the consuming project to verify it picks up the new TGP.Data:
   ```
   dotnet build <project>.csproj
   ```

7. Commit and push the consuming project changes

8. Update monorepo submodule references if needed:
   ```
   git submodule update --remote <submodule-name>
   git add -A && git commit -m "chore: Update submodule references"
   git push origin main
   ```

## Windows Client DateTime Rule
- **NEVER use `DateTime.UtcNow`** in client code
- Always use `ITimeProvider` for time operations (NTP-synced)
