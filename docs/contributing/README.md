# Contributing

How to build, test and document Dataphor. Three short pages; this one is the setup and the map.

For what Dataphor is and what it does, read the [repository README](../../README.md) — it is not repeated here. For how the system fits together, [Architecture](../architecture.md); for where the port stands and what is left, the [modernization plan](../modernization-plan.md).

## What you need

- **The .NET 10 SDK, and nothing else.** `global.json` pins version 10.0.100 with `rollForward: latestFeature`, so any 10.0.1xx SDK will do. There is no Visual Studio requirement, no Node and no pandoc: `dotnet build` and `dotnet test` are the whole toolchain. A missing or too-old SDK shows up as `dotnet` refusing to run in the repository at all, which is what the pin is for.
- **Python 3**, for the two documentation checks and a handful of repository tools. They use the standard library only, so there is nothing to install.
- **Windows.** The repository is developed and tested on Windows: the client and the IDE are WinForms, the runtime layout is assembled for Windows x64, and continuous integration builds and tests on `windows-latest`. Nothing here has been exercised on another operating system.
- **Optional: a database server.** Most of the suite needs none. A SQL Server and a PostgreSQL unlock the passes that talk to one; [Build and test](build-and-test.md) says which, and how to point the tests at them.

## Which solution

`Dataphor.slnx`, at the repository root.

The older `Dataphor.sln` beside it is the .NET Framework solution. It no longer builds — its projects reference the converted `net10.0` ones — and it is deleted when the legacy build goes. Always name the `.slnx`.

## Where the build puts things

Nothing is built into a source folder. Everything goes under `artifacts/`, which is git-ignored, so a build leaves the tree clean:

| Path | What |
|---|---|
| `artifacts/bin/<project file name>/<debug\|release>/` | Assemblies and executables. The folder is keyed by project *file* name, so no two project files in the solution may share one. |
| `artifacts/obj/` | Intermediate files. |
| `artifacts/layout/<debug\|release>/` | The runtime layout: an assembled server with its libraries, which the tests and a local run both start from. |

Shared build settings are in `Directory.Build.props` and package versions in `Directory.Packages.props`, both at the root.

## The pages here

- **[Build and test](build-and-test.md)** — the build commands and what they cost, how to run part of the test suite instead of all of it, the runtime layout, and the environment variables that turn tests on or make them talk.
- **[Writing documentation](writing-docs.md)** — what the documentation under `docs/` is made of, where a new page goes, and the two checks that gate it.

A release checklist belongs in this folder too. It is not written yet: it needs an installer and a release workflow to describe, and the work item that writes it (`release-checklist-and-changelog`, in WS9 of the plan) waits on both.

## Where the rest is written down

[AGENTS.md](../../AGENTS.md) at the repository root is the fuller operating manual — the project layout in detail, which tests to run when, the compatibility surfaces that may not change without a ticket, and the conventions a converted project follows. These pages link into it rather than repeating it, because a fact written twice is a fact that goes stale in one of the two places.

Work is planned and tracked as tickets under [tickets/](../../tickets); [docs/README.md](../README.md) indexes the documentation; [migration notes](../migration-notes.md) list the changes an application, a deployment script or a custom extension will notice.
