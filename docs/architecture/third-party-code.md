# Third-party code in the tree

The third-party libraries and binaries the tree carries, and which version each is. Part of the [Dataphor architecture](../architecture.md).

ICSharpCode.TextEditor, DockPanel Suite (WeifenLuo) and Ascend controls, Npgsql 2.0.4 with Mono.Security (all binaries in `external/`), ADODB interop (used only by parked code under `legacy/`), System.Data.SQLite (NuGet `System.Data.SQLite.Core` 1.0.119, with its native interop for each architecture; 3.1 used a checked-in 1.0.60, 32-bit only), Newtonsoft.Json (NuGet 13.0.4; 3.1 used 9.0.1), Hl7.Fhir 0.9x (NuGet; used only by parked code under `legacy/`), plus a complete copy of WiX 3.5 under `Utilities/`. The NuGet versions of the converted projects are in `Directory.Packages.props`.
