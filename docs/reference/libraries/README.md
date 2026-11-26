# Libraries

## Summary

Reference material for the libraries this release ships, one page each. A library is Dataphor's unit of deployment: a descriptor (`.d4l`) naming its version, the libraries it needs and the files it carries, plus the D4 and C# that make up its contents. These pages describe what each library is for, what it needs, what files and classes it registers, and — where the library has them — its settings, scalar types and operators.

The core library, `System`, is documented separately and at much greater length, because its 297 operators and 20 scalar types are the D4 language's own vocabulary rather than one library's contribution: see [the System library reference](../system-library/README.md).

## The libraries

| Library | What it is for |
|---|---|
| [Connection](Connection.md) | The connectivity layer the SQL devices use to reach a target system: the SQL Server and OLE DB connection classes, and `ClearAllPools`. |
| [Frontend](Frontend.md) | The server half of the user-interface layer: the Applications and Documents tables, form derivation, and the operators a client calls to load, save and derive form documents. |
| [General](General.md) | A library with no contents of its own: no files, no D4, nothing but a descriptor that pins no version and requires `System`. |
| [MSSQLDevice](MSSQLDevice.md) | The SQL device for Microsoft SQL Server: its own settings, the `MSSQLBinary` scalar type and the comparison operators on it. |
| [ODBCConnection](ODBCConnection.md) | An ODBC connection class for the SQL devices. |
| [PGSQLConnection](PGSQLConnection.md) | The Npgsql-based connection class the PostgreSQL device uses. |
| [PGSQLDevice](PGSQLDevice.md) | The SQL device for PostgreSQL. |
| [Security](Security.md) | The forms and the D4 behind them for administering users, roles, rights and device users; it creates the `SecurityUser` role. |
| [SimpleDevice](SimpleDevice.md) | The memory device with file persistence: its settings, and the operators that save, load and truncate a table. |
| [SQLDevice](SQLDevice.md) | The generic SQL device every specific one derives from: the settings common to all of them, the `SQLText` scalar type, and the operators that inspect or pass through SQL. |
| [System.Integration](System.Integration.md) | Bulk data movement between tables: `Copy`, `CopyTables` and `Insert`. |
| [System.Internet](System.Internet.md) | Operators that reach out over the network and parse what comes back: HTTP and FTP, HTML and URL encoding, XML, and the dynamic operators over JSON values. |
| [System.Platform](System.Platform.md) | The file system and process operators: file and directory manipulation, attributes, and `PlatformExecute`. |

## What these pages do not say

**The list of objects a library defines is not here.** Each library chapter of the 2007 documentation ended in a *Schema* section — the forms, roles, devices, types and tables the library defines — and that section was filled from a running server at publication time, not written by hand. There is no reliable way to reproduce it from the source tree, so it has been left out rather than published empty or stale. Generating it from a live catalog belongs to a later piece of work; the operators, types and settings on these pages are the hand-written parts of those chapters and are complete.

**A *Registered Classes* section appears only where the list is fixed.** A library registers its D4 class names — the strings an application writes in `class "…"` — from its assembly's `DAERegister.GetClasses()`. Three libraries name their classes one by one, and those three pages list them. Eight build the list by reflecting over every type in the assembly, so the names depend on the build and not on any file that can be read here; `General` and `Security` ship no assembly and register nothing. Of the ten pages with no fixed list, the eight converted from the 2007 chapters carry no such section at all, and the two written by hand for this release — `PGSQLDevice` and `System.Platform` — name the namespace the classes land in instead of listing them. That namespace is readable even where the list is not: each library's `Source/**/Register.cs` declares it as `D4ClassDefinitionNameSpace`, and it is the library's own name for every library but `PGSQLConnection` (whose classes are `PostgreSQLConnection.…`) and `PGSQLDevice` (`PostgreSQLDevice.…`). The list itself waits on a live catalog.

**The version, requisites, file list and registered assemblies come from this repository, not from the 2007 help file.** The published help gives SimpleDevice version 2.1.0.0, for instance, where the shipped descriptor says 3.0.0.0. These four facts were read from each library's `.d4l` descriptor under `libraries/`, which remains the authoritative statement of them.

**Five libraries have no *Operators* section, including three that do have operators.** Each library chapter of the 2007 documentation pulled its operator descriptions in from a separate file, and five of those files are not in this repository: the ones for `Connection`, `General`, `ODBCConnection`, `Security` and `System.Internet`. `General` and `ODBCConnection` define no operators, so nothing is missing there. The other three do — `Connection` declares `ClearAllPools`, `Security` declares twenty operators behind its administration forms, and `System.Internet` declares forty-four, the network, encoding and JSON operators the table above describes — and for those the descriptions were either never written or lost before this repository was made. Until a page can be generated from a running server, the authoritative list of what a library defines is its own D4 under `libraries/<Library>/Documents/` — `Register.d4` and, for `System.Internet`, `Dynamic.d4` beside it.

**Libraries this release does not ship are not documented.** The 2007 reference also had chapters for the ADO, Microsoft Oracle, Oracle, DB2 and SAS libraries. The first four are parked under `legacy/libraries/`, DB2 is gone from the tree, and a page about a library nobody can load would be worse than no page; links to those chapters are left as plain text. A sentence that still mentions one of them — the SimpleDevice page's *See Also*, and the list of device-specific settings on the SQLDevice page — reads as a reference to something that no longer exists. Those sentences are the 2007 text and were left as they stand rather than rewritten.
