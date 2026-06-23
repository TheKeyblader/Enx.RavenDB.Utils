# Enx.RavenDB.Utils

Utility helpers and extensions for the [RavenDB](https://ravendb.net/) .NET client.

[![CI](https://github.com/TheKeyblader/Enx.RavenDB.Utils/actions/workflows/ci.yml/badge.svg)](https://github.com/TheKeyblader/Enx.RavenDB.Utils/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/v/Enx.RavenDB.Utils.svg)](https://www.nuget.org/packages/Enx.RavenDB.Utils/)

## Installation

```bash
dotnet add package Enx.RavenDB.Utils
```

## Usage

### Ensure a database exists

```csharp
using Enx.RavenDB.Utils;

var created = await store.EnsureDatabaseExistsAsync("my-database");
```

## Building

```bash
dotnet build
dotnet test
```

The repository uses:

- **`.slnx`** modern solution format
- **Central Package Management** (`Directory.Packages.props`)
- **Shared build configuration** (`Directory.Build.props`)
- **SourceLink** for debuggable NuGet packages
- **SDK pinning** via `global.json`

## Contributing

Pull requests are welcome. Please add release notes to `RELEASE_NOTES.md` and keep tests green.

## License

[MIT](LICENSE)
