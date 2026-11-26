# System Tables

The System library defines 55 table variables - 48 tables and 7 views - and they are the catalog itself, readable as data: an ordinary `select` against any of them answers a question about the server's own objects, and `select TableVars` is the list below. There is a page here for each one, carrying the definition a running server reports.

**These pages are generated, and every one of them is compared whole.** `ReferenceSignatureGoldenTest` in `tests/DAE.Server.Tests` starts a server carrying nothing but the built-in System library, asks it for each table's definition through the `Script` operator any script can call, and holds the result against the whole of the matching page - this index included. So a hand edit in this folder fails the test run, and is reverted the next time the pages are written. Change what the generator writes, not the page.

**There are no descriptions here, and none were invented.** The 3.x reference meant to carry a sentence about each table and each column; it never did - its `SystemTables` chapter was a four-line placeholder saying the section was generated, and the published help file listed the table names with nothing under them. The catalog has nothing to report either: no table or column of `src/engine/DAE/Schema/SystemCatalog.d4` carries a `Catalog.Comment` tag, where the scalar types in `DataTypes.d4` carry seventeen. A sentence per table written from a reading of the engine's source would be a new claim in the place of a converted record, so each page gives the definition and stops. For what the catalog holds and how a script uses it, read [Table Variables](../../../developers-guide/d4-catalog.md#table-variables) and the rest of the [Catalog Elements](../../../developers-guide/d4-catalog.md) chapter of the Developer's Guide.

**What a definition does not carry: the references.** `Script` emits a table's columns, keys, orders, constraints and tags, and not the references declared beside it - in this release a reference is a catalog object in its own right, and the [System.References](System.References.md) table carries each one's name, library and owner rather than the two tables it connects. There are 57 of them in the System library. `Script('System.TableVars_CatalogObjects')` on a live server gives any one of them in full.

## Tables

- [System.ApplicationTransactionOperatorMaps](System.ApplicationTransactionOperatorMaps.md)
- [System.ApplicationTransactionOperatorNameMaps](System.ApplicationTransactionOperatorNameMaps.md)
- [System.ApplicationTransactionTableMaps](System.ApplicationTransactionTableMaps.md)
- [System.ApplicationTransactions](System.ApplicationTransactions.md)
- [System.BaseTableVars](System.BaseTableVars.md)
- [System.CatalogConstraints](System.CatalogConstraints.md)
- [System.CatalogObjects](System.CatalogObjects.md)
- [System.Connections](System.Connections.md)
- [System.DerivedTableVars](System.DerivedTableVars.md)
- [System.DeviceOperators](System.DeviceOperators.md)
- [System.DeviceScalarTypes](System.DeviceScalarTypes.md)
- [System.DeviceSessions](System.DeviceSessions.md)
- [System.DeviceUsers](System.DeviceUsers.md)
- [System.Devices](System.Devices.md)
- [System.Libraries](System.Libraries.md)
- [System.LibraryFileEnvironments](System.LibraryFileEnvironments.md)
- [System.LibraryFiles](System.LibraryFiles.md)
- [System.LibraryOwners](System.LibraryOwners.md)
- [System.LibraryRequisites](System.LibraryRequisites.md)
- [System.LibrarySettings](System.LibrarySettings.md)
- [System.LibraryVersions](System.LibraryVersions.md)
- [System.LoadedLibraries](System.LoadedLibraries.md)
- [System.ObjectDependencies](System.ObjectDependencies.md)
- [System.Objects](System.Objects.md)
- [System.Operators](System.Operators.md)
- [System.Plans](System.Plans.md)
- [System.Processes](System.Processes.md)
- [System.References](System.References.md)
- [System.RegisteredAssemblies](System.RegisteredAssemblies.md)
- [System.RegisteredClasses](System.RegisteredClasses.md)
- [System.RemoteSessions](System.RemoteSessions.md)
- [System.Rights](System.Rights.md)
- [System.RoleRightAssignments](System.RoleRightAssignments.md)
- [System.Roles](System.Roles.md)
- [System.ScalarTypes](System.ScalarTypes.md)
- [System.Scripts](System.Scripts.md)
- [System.ServerLinkUsers](System.ServerLinkUsers.md)
- [System.ServerLinks](System.ServerLinks.md)
- [System.ServerSettings](System.ServerSettings.md)
- [System.SessionCatalogObjects](System.SessionCatalogObjects.md)
- [System.Sessions](System.Sessions.md)
- [System.Sorts](System.Sorts.md)
- [System.TableDee](System.TableDee.md)
- [System.TableDum](System.TableDum.md)
- [System.TableVars](System.TableVars.md)
- [System.UserRightAssignments](System.UserRightAssignments.md)
- [System.UserRoles](System.UserRoles.md)
- [System.Users](System.Users.md)

## Views

- [System.AttachLibraries](System.AttachLibraries.md)
- [System.AttachLibrary](System.AttachLibrary.md)
- [System.DetachLibrary](System.DetachLibrary.md)
- [System.DevicesNameAndLibrary](System.DevicesNameAndLibrary.md)
- [System.LibraryView](System.LibraryView.md)
- [System.ListLogs](System.ListLogs.md)
- [System.ViewLog](System.ViewLog.md)

**See Also**

[System Library](../README.md) \| [Catalog Elements](../../../developers-guide/d4-catalog.md) \| [Dataphor Reference](../../README.md)
