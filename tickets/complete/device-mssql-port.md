description: The product now reaches SQL Server through Microsoft's current database driver instead of the retired one, and a server or catalog connection that worked in the previous release still connects, because the new driver demands an encrypted connection with a trusted certificate where the old one asked for neither.
architecture: docs/modernization-plan.md#ws7-devices-and-libraries
files: Directory.Packages.props, src/engine/Server/Connection/MSSQLConnection.cs, src/engine/Server/Connection/MSSQLCommand.cs, src/engine/Server/Connection/MSSQLServerCursor.cs, src/engine/Server/Server.csproj, src/engine/Server/Server/Server.cs, src/engine/DAE/Server/Engine.cs, src/engine/DAE/DAE.csproj, src/engine/Store.MSSQL/MSSQLStore.cs, src/engine/Store.MSSQL/MSSQLStoreConnection.cs, src/engine/Store.MSSQL/DAE.MSSQL.csproj, src/engine/Store.PGSQL/PGSQLStore.cs, libraries/Connection/Source/Operators.cs, libraries/Connection/Source/DAE.Connection.csproj, libraries/Connection/Documents/Register.d4, libraries/MSSQLDevice/Source/Device.MSSQL/MSSQLConnectionStringBuilder.cs, tests/libraries/LibraryLoadingProbe/Source/LibraryLoadingProbe.csproj, tests/DAE.Server.Tests/MSSQLStoreConnectionStringTest.cs, tests/DAE.Server.Tests/DeviceTranslationGoldenTests.cs, tests/DAE.Server.Tests/MSSQLDeviceTranslationGoldenTests.cs, tests/DAE.Server.Tests/StoreContractTests.cs, tests/DAE.Server.Tests/DAE.Server.Tests.csproj, docs/migration-notes.md, docs/architecture/devices.md, docs/architecture/catalog-and-store.md, docs/architecture/third-party-code.md, docs/users-guide/dataphor-service.md, docs/modernization-plan.md, AGENTS.md
----
## What landed

Built in `ticket(implement): device-mssql-port` and adjusted by this review pass.

**One SQL Server client.** `System.Data.SqlClient` is gone from the tree; `Microsoft.Data.SqlClient` 7.1.2 is pinned in `Directory.Packages.props`, and `Microsoft.Data.SqlClient.SNI.runtime` - the native network interface that client depends on - moved 4.0.0 -> 7.1.2 with it, because central package management allows one version per package id. Six files changed a `using` and nothing else: the three connection classes in `src/engine/Server/Connection/`, `MSSQLStore.cs`, `MSSQLStoreConnection.cs` and `libraries/Connection/Source/Operators.cs`. The long `SqlDbType` switch in `MSSQLCommand.PrepareParameters` needed nothing, because `SqlDbType` is `System.Data.SqlDbType` on both clients; `IsTransactionFailure` and `IsUserCorrectableError` needed nothing either, because they switch on SQL Server error numbers and `SqlException.Errors`/`SqlError.Number` keep their shape under the new namespace.

**The engine assembly no longer references a SQL client.** `Engine.Dispose`'s `SqlConnection.ClearAllPools()` moved to a new `Dispose(bool)` override on `Server` and the package reference left `DAE.csproj`. The server assembly is where the connection classes live, so it is the assembly that can have opened a SQL Server connection and the one that should clear its pools. The call is after `base.Dispose(disposing)`, so the order is 3.1's, and in a `finally`, because the statement it moved out of was in one: a `Stop()` that throws on the way down cleared the pools before the failure propagated, and still does.

**The compatibility default, in the two places a SQL Server connection string is built.** `Encrypt=False`, not `TrustServerCertificate=True`: only the first is what release 3 negotiated, while the second encrypts and then accepts any certificate, which is a *new* behaviour and would quietly change what every existing deployment does. On the device it is a parameter of `MSSQLADODotNetConnectionStringBuilder`, which gets the precedence right by construction, because `SQLDevice.GetConnectionParameters` applies the device's and the device session's own `ConnectionParameters` after the builder has run, so `'ConnectionParameters' = 'Encrypt=True'` wins. In the store it is a conditional default in `MSSQLStore.InternalInitialize`, applied only when the connection string names neither `Encrypt` nor `TrustServerCertificate`, because a deployment that set either has already said how that connection is protected. Each site carries the reasoning and the override.

**`MSSQLConnection.IsZombied`.** `SqlTransaction.IsZombied`, which `MSSQLConnection` reads on every commit and rollback, is present on 7.1.2 as an internal instance `Boolean` property. The reflection stays, but the lookup is now a cached static `PropertyInfo`, and both ways it can fail - the member gone, or a transaction from some other client - throw a `DataphorException` (severity System) naming the property, the declaring type, the assembly and which half failed, instead of dereferencing null on every commit and rollback.

**The library-loading probe.** `Microsoft.Data.SqlClient` pulls the SNI runtime package into `Server.csproj`'s closure, so the layout stopped treating that package as private to `LibraryLoadingProbe` (which referenced the server assembly), the native dll landed in `bin/` instead of flat in the library's folder, and `LibraryLoadingTest.NativePrivateDependencyResolvesFromLibraryFolder` failed. The fix was not a different package: the probe never used the server assembly at all - its library manifest names only its own dll and the `System` library, and its nodes need `DAE` and `Base` - so the unused `ProjectReference` is gone and the package is private again, with a `NOTE:` in the project file saying why the reference must not come back.

**`PGSQLStore.cs`** lost an unconditional dead `using`. `PGSQLStoreConnection.cs` was left alone, because its `using` sits inside the uncompiled `#else` branch next to the `SqlConnection` call it guards, and that branch is `store-pgsql-port`'s - so one grep hit for the old namespace remains in the tree deliberately, in a project that is not in the solution.

## How to exercise it

```
dotnet build Dataphor.slnx
dotnet test tests/DAE.Server.Tests --no-build --filter "FullyQualifiedName~MSSQLStoreConnectionStringTest|FullyQualifiedName~MSSQLDeviceTranslationGoldenTests|FullyQualifiedName~LibraryLoadingTest|FullyQualifiedName~HostProgramTest"
```

Against a real SQL Server, which is the only way the client itself is driven:

```
sqllocaldb start MSSQLLocalDB
DATAPHOR_TEST_MSSQL='Data Source=(localdb)\MSSQLLocalDB;Integrated Security=SSPI' dotnet test tests/DAE.Server.Tests --no-build --filter "FullyQualifiedName~MSSQLStoreContractTests"
```

## Tests this ticket added

| Test | What it pins |
|---|---|
| `MSSQLStoreConnectionStringTest.AConnectionStringThatSaysNothingAboutEncryptionGetsTheOldClientsBehaviour` | a release-3 `catalogstoreconnectionstring` comes out of `InternalInitialize` with `Encrypt=False` |
| `...AConnectionStringThatAsksForEncryptionKeepsIt` | a deployment's `Encrypt=True` survives - the store defaults, it does not force |
| `...AConnectionStringThatTrustsTheServerCertificateIsLeftAlone` | `TrustServerCertificate=True` counts as having chosen, so no `Encrypt` is added beside it |
| `...MultipleActiveResultSetsIsForcedOverADeploymentsFalse` | the other rewrite in the same method is still *forced*, which is the difference the fixture exists to hold |
| `MSSQLDeviceTranslationGoldenTests.EveryConnectionCarriesTheDevicesCompatibilityDefaults` | every connection string the device built for a definition naming only server, database and integrated security carries `Encrypt=False` |

The store fixture needs no server: with `ShouldEnsureDatabase` off, initializing the store only rewrites its connection string. Every assertion reads the result back through `DbConnectionStringBuilder` rather than matching text, because that class lower-cases the keywords it parsed out of the supplied string and leaves the ones the store added alone. The device case went in through a `protected virtual string[] RequiredConnectionStringText` on the shared golden base, empty by default, so `device-pgsql-device-golden`'s subclass can state its own defaults or none.

## Review findings

### What was checked, and how

The implement diff was read before the handoff summary. Beyond reading it:

- **The device's precedence claim was verified rather than taken.** `ConnectionStringBuilder.Map` applies `_parameters` *after* mapping the incoming tags, so a builder parameter beats a device property; the device session's connection path then calls `SQLDevice.GetConnectionParameters`, which `AddOrUpdate`s the device's and the session's `ConnectionParameters` over the mapped tags. So the comment is right: the deployment's setting wins. The incoming tag set is fixed (`ServerName`, `DatabaseName`, `ApplicationName`, and either `IntegratedSecurity` or `UserName`/`Password`), so `ConnectionParameters` is the only route by which a definition can say anything about encryption, and it is the route that wins.
- **`SqlTransaction.IsZombied` was confirmed on the pinned package**, by reflecting over `Microsoft.Data.SqlClient` 7.1.2 in a throwaway project outside the repository: an instance `System.Boolean` property, readable, non-public. The live store-contract run below then exercised it for real.
- **Duplicate connection-string keywords were measured**, because D4 tag names compare case-sensitively and `'ConnectionParameters' = 'encrypt=true'` therefore appends a second keyword rather than replacing the builder's. `SqlConnectionStringBuilder` takes the **last** occurrence and is case-insensitive about which keyword it is (`Encrypt=False;encrypt=True` parses as `Encrypt=True`), so the override holds whatever case a deployment writes it in.
- **`Server.Dispose(bool)` is safe to override as written.** `Disposable` compiles without `USEFINALIZER`, so `disposing` is always true and the pool clear never runs on a finalizer thread; no type in the tree derives from `Server`, so the new link in that chain is only visible to an out-of-tree extension, which the migration notes record.
- **The probe's dropped project reference is sound.** `LibraryLoadingProbe.d4l` names only its own assembly and the `System` library, and `Documents/Register.d4` registers only its own two nodes, so nothing needed the server assembly at run time either.
- **The whole tree was grepped for the old namespace**, which turned up one hit the handoff had not accounted for (below).
- **Every document the change touches was read**, plus `docs/architecture/catalog-and-store.md`, which it should have touched.

### Found and fixed in this pass

- **`libraries/Connection/Documents/Register.d4` still described the old client.** The comment above `create operator ClearAllPools()` said the operator calls `System.Data.SqlClient.SqlConnection.ClearAllPools()` - a class no longer in the tree. This is the shipped D4 registration document a user reads, and the handoff's claim that one deliberate grep hit remained was therefore one short. Comment corrected; the statement is untouched.
- **The override guidance in `MSSQLConnectionStringBuilder.cs` named a setting that does not work.** It offered `'ConnectionParameters' = 'Encrypt=True' (or Strict, or TrustServerCertificate=False)` as the way to get encryption back. `TrustServerCertificate` on its own changes nothing there, because the builder's `Encrypt=False` still stands beside it and an unencrypted connection presents no certificate to trust. Replaced with `Encrypt=Strict` and `Encrypt=True;TrustServerCertificate=True`, which is also what `docs/users-guide/dataphor-service.md` tells a deployment for the store, and the dead alternative is now called out as dead. The store's half of the migration note was already right, because for the store `TrustServerCertificate=True` alone *does* mean encryption - the store then adds no `Encrypt` and the client's own default applies.
- **`MSSQLServerCursor.cs` carried a dead `using`.** The file uses no type from the SQL client namespace and has no conditionally-compiled branch that needs one, so the ticket had renamed a dead directive rather than removing it. Removed; warning counts unchanged.
- **The new golden case duplicated its sibling's recorder read, without the caveat that makes it safe.** `TheDeviceConnectedOnlyThroughTheRecorder` carries a `NOTE:` explaining why `SQLRecorderConnections()` is non-empty at all - each data-definition statement opens a fresh connection because `SQLDeviceSession.ReleaseConnection` disposes an idle execute-pool connection - and `EveryConnectionCarriesTheDevicesCompatibilityDefaults` repeated the read and the non-empty assertion without it. Both now call one `RecordedConnections()` helper that carries the note, so the next reader meets the constraint once and a change to that pooling fails both cases with a message that explains itself.
- **`docs/architecture/catalog-and-store.md` named one of the store's two connection-string rewrites.** The page that owns the catalog store said it forces `MultipleActiveResultSets=True` and said nothing about the `Encrypt` default added right beside it. Both are now described there, pointing at `devices.md` for the decision and the device's half of it. Word caps still pass.

### Appended as an arm to an existing ticket

- **`test-device-live` is where the unproven half of this ticket goes, and it did not yet ask for it.** That ticket plans to parse `DATAPHOR_TEST_MSSQL` into the device's `ServerName` and credentials, which would silently drop an `Encrypt` or `TrustServerCertificate` the variable carries - so a server that *requires* encryption would be unreachable by the live device. Two bullets and two TODO lines added there: carry those keywords through as the device's `ConnectionParameters`, and record what the `Encrypt=False` default and its override actually do against a server whose certificate this machine does not trust, saying plainly if no such server was available. No new ticket: the site and the run are already that ticket's.

### Checked, and resolved without a change

- **The handoff's open question about the OLE DB and ODBC connection-string builders is not a reachable configuration.** A device that keeps `Connection.MSSQLConnection` but names `MSSQLOLEDBConnectionStringBuilder` or `MSSQLODBCConnectionStringBuilder` gets no compatibility default - but it never worked: measured, `SqlConnectionStringBuilder` rejects both builders' output outright (`Keyword not supported: 'provider'`, `Keyword not supported: 'dsn'`), as the old client did. The default reaches every pairing that can connect at all. Nothing filed and nothing noted, because there is no behaviour here to protect.
- **The store's two-key condition is the right pair.** `Encrypt` covers a deployment that wrote `Encrypt=Strict`; `TrustServerCertificate` covers the other way of letting an untrusted certificate through. A deployment that wrote `TrustServerCertificate=True` for release 3 connected unencrypted then and will connect encrypted now - a change the site's comment states, and the right one, because the alternative is to contradict a setting the deployment chose. No third keyword in the client's vocabulary means "how this connection is protected" without one of these two.
- **The engine no longer clearing SQL Server pools on dispose** is a real behaviour change for an out-of-tree extension that hosts a bare `Engine` and opens connections through `Connection.MSSQLConnection`. `docs/migration-notes.md` carries the row, naming what to call instead. Left as it is.
- **`MSSQLStore.cs`'s top-level `using` is dead in the compiled configuration but must stay**, because the `#else` branch inside `InternalInitialize` uses `SqlConnection` and `SqlCommand` and a `using` directive cannot live inside a method body. Not the same case as the `PGSQLStore.cs` directive the ticket removed.
- **`RequiredConnectionStringText` being `virtual` with an empty default** rather than `abstract` leaves a vacuously-passing case for a device that defaults nothing. Considered and left: the guard would still be needed once a PostgreSQL subclass declares `[]`, so `abstract` would move the explicitness without removing the branch.

### Tripwires parked, not filed

- **Nothing CI runs exercises the `IsZombied` reflection.** `NOTE:` at the site in `MSSQLConnection.cs`: the only cases that commit or roll back through this class are `MSSQLStoreContractTests`, skipped unless `DATAPHOR_TEST_MSSQL` names a server, and CI configures none - so a client upgrade that drops the member would build and pass, and the new throw would first be seen on a live run. `test-device-live` is the ticket that would put a server in CI.
- **The client's dependency closure**, parked by the implement stage as a `NOTE:` in `src/engine/Server/Server.csproj`: eight of the added assemblies exist for authentication methods nothing here uses (the Microsoft Entra ID path) and ship because they are in the package's dependency graph. Fine now; if the installer's size or a deployment's dependency audit objects, the question is whether a trimming step belongs in WS9's installer, since NuGet has no supported way to prune a package's own dependencies. Re-read and left as it stands.

### Nothing filed

No ticket was filed. Of the findings above, five were comment, document or test-shape defects fixed in this pass; one belonged to an existing ticket as an arm; one is a conditional concern now recorded at its site. Nothing reached the filing bar - no latent defect, no class-level invariant going unprotected, and no architecture question the plan has not already answered.

## What was run

Clean builds, `--no-incremental`, both configurations: **0 errors, 127 warnings each**, unchanged by this pass and down from 144 before the client swap - the drop is the 17 `CS0618` warnings `System.Data.SqlClient` 4.9.1 raised at every call site, because that package marks its types obsolete. No suppression anywhere.

Tests, Debug, `--no-build`. The change alters `Directory.Packages.props` and the shipped runtime layout for both host executables, so this pass ran the three fixture parts from `AGENTS.md` rather than a filter, which is every NUnit fixture in the solution:

- part 1 (everything outside the `DAE.Server.Tests` namespace, plus its classes outside `A`-`N`) - **495 passed, 0 failed**, 19 skipped, 3 m 11 s.
- part 2 (`[ABD-N]`) - **262 passed, 0 failed**, 14 skipped, 3 m 5 s.
- part 3 (`C` without `CoverageScripts`) - **210 passed, 0 failed**, 3 m 56 s.
- `DATAPHOR_TEST_MSSQL` set to `Data Source=(localdb)\MSSQLLocalDB;Integrated Security=SSPI`, `--filter "FullyQualifiedName~MSSQLStoreContractTests"` - **14 of 14 passed live**, 0.74 s, after `sqllocaldb start MSSQLLocalDB`. The transaction cases commit and roll back through `MSSQLConnection`, so the `IsZombied` reflection ran repeatedly against a real `SqlTransaction` and answered.
- the engine-area coverage scripts the implement stage ran, `--filter "Name~Transaction|Name~SystemCatalog|Name=Devices|Name~CatalogStore"` - 92 tests, **83 passed, 9 skipped**, 3 m 36 s, with the known failures matched exactly (`4 matched, 0 mismatched, 0 unexpectedly passing`): the three SQL Server device atomicity scripts (`feat-ws7-devices-and-libraries`), `Transaction.Isolation.Run` (`debt-memory-device-transaction-isolation`), and the five store-contract cases this run had no variable set for.
- after the last edit, `--filter "FullyQualifiedName~MSSQLDeviceTranslationGoldenTests|FullyQualifiedName~MSSQLStoreConnectionStringTest|FullyQualifiedName~LibraryLoadingTest|FullyQualifiedName~HostProgramTest|FullyQualifiedName~SQLiteStoreContractTests"` - 30 of 30 passed, 12 s.

No pre-existing failure surfaced, so no `tickets/.pre-existing-error.md` was written. `git status tests/goldens/` is empty: the device translation is unchanged, which says nothing at all about execution.

`python tools/docs-size-check/check_size.py` - 13 files, 0 over the limit. `python tools/docs-link-check/check_links.py docs --pending tools/docs-convert/legacy-links.tsv` - 0 errors, 203 pending, all pre-existing.

## What is not verified, plainly

- **The compatibility trap itself never fired here.** The only SQL Server on this machine is LocalDB, and this driver treats a local connection's certificate as trusted: measured, `Encrypt` unset, `Encrypt=False` and `Encrypt=True` all connect to `(localdb)\MSSQLLocalDB`. So the live run proves the new client works; it does **not** prove the default is needed, and it does not exercise the override. Both now belong to `test-device-live` by name, against a server with a self-signed or absent certificate - the run that would also catch it if `Encrypt=False` were the wrong spelling for this driver version.
- **The device's server-side cursors compile and are unverified.** `MSSQLCommand`'s `sp_cursoropen`/`sp_cursorfetch` route, its fixed internal-parameter order, its `ParameterDirection.ReturnValue` and `NVarChar` out parameters, and `MSSQLServerCursor`'s fetch loop: nothing in the suite drives any of it, before or after this change. `test-device-live` owns the proof.
- **No device ran against a real SQL Server at all.** The goldens replace the client; the store contract exercises the client but not the device.
- The `varchar` parameter mapping is untouched, deliberately, so that this change stayed a swap. `device-mssql-string-parameters` owns it.

## Documentation

`docs/migration-notes.md`: the device row and the namespace row went `planned` -> `done`, each saying what the default is and how a deployment overrides it, and a third row covers the `ClearAllPools` move. `docs/architecture/devices.md` gained where the client lives, why that assembly owns clearing its pools, and the two connection defaults with their precedence. `docs/architecture/catalog-and-store.md` now describes both of the store's connection-string rewrites (this pass). `docs/architecture/third-party-code.md` names the client and its native network interface with versions. `docs/users-guide/dataphor-service.md` gained the third thing to know about the SQL Server catalog store. `AGENTS.md` notes that `DATAPHOR_TEST_MSSQL` is used as written, so a remote server with an untrusted certificate needs `Encrypt=False` in it. The plan records the result in the WS7 library row, the `device-mssql-port` work-item row and the WS4 `store-mssql-port` row.
