description: The administrator documentation now describes every setting in the server's instance configuration file — what each one does, what it defaults to and why an operator would change it — and the stale table that described settings the product no longer has is gone.
architecture: docs/modernization-plan.md#ws5-client-server-protocol-and-hosting
files: docs/users-guide/dataphor-service.md, docs/users-guide/dataphor-server.md, docs/architecture.md, docs/modernization-plan.md, src/engine/Server/Server/ServerConfiguration.cs, src/engine/Server.Hosting/DataphorServiceHost.cs
----
## What landed

A reference section for the computer's list of Dataphor Server instances — the file `ServerConfigurations.config` — written as administrator documentation, plus the removal of a table that described settings the product does not have. Implemented by `ticket(implement): host-instance-configuration` and amended in review as the findings below record.

**The new section.** `## Instance Configuration` in `docs/users-guide/dataphor-service.md`, between "Installing Multiple Instances" and "Uninstalling", anchored at `#instance-configuration`, in six sub-sections: where the file is; when it is read and written; the settings (a quick-reference table plus a paragraph per setting); device settings; a worked example taken from the serializer's own output; and what happens to a file an earlier release wrote.

**The stale table is gone.** The seven-row HTML table under "Service Configuration Utility → Configuration" — four of whose rows (Catalog Store Database Name, Catalog Store Password, Tracing Enabled, Log Errors) described settings that do not exist in `ServerConfiguration` — is replaced by one sentence saying the form edits this service's instance's entry in the computer's instance list, and a link to the new section. The screenshots, the "Other Options" table and the troubleshooting table are untouched; the screenshots still show the 3.1 fields, which is `ui-config-utility`'s to fix.

**Four cross-references point at the section:** the "Installing Multiple Instances" bullet that already names the file, `docs/users-guide/dataphor-server.md`'s "Hosting a Dataphor Server", `docs/architecture.md`'s on-disk table row for the file, and (added in review) the plan's own WS5 row.

**Two `NOTE:` tripwires in code, no behaviour change.** One above the `[DefaultValue]` cluster in `src/engine/Server/Server/ServerConfiguration.cs`, recording that those four attributes decide what a saved instance list contains and that the user-guide section documents it, so adding or removing one needs the documentation amended in the same change. One at `DataphorServiceHost.BuildHost`, added in review, for the unchecked port number; see *Tripwires* below.

## What the section says, and where each claim comes from

Every value was read out of the source rather than copied from the plan ticket's table, and the review re-derived each one independently. The sites:

| Claim | Site |
|---|---|
| Instance list path `%ProgramData%\Alphora\Dataphor\4\Instances\ServerConfigurations.config` | `InstanceManager.ResolveInstanceDirectory`, `InstanceConfigurationFileName`, `Server.DefaultInstanceDirectory` |
| Folder created on first look-up, file only on save | `InstanceManager.GetInstanceDirectory` creates; `InstanceConfiguration.Load` returns an empty configuration for a missing file |
| Settings read at instance start and not again | `DataphorServiceHost.Start` calls `InstanceManager.Load` then `ServerConfiguration.ApplyTo` |
| An unknown instance gets a default entry that is then saved | `DataphorServiceHost.Start` |
| The listener answers out of the file, not out of running servers | `ListenerService.GetConfiguration` → `InstanceManager.LoadConfiguration` |
| `name` case-insensitive and unique; a nameless or duplicate entry is dropped whole, the rest of the file still read | `InstanceList : HashtableList<string, ServerConfiguration>` with `OrdinalIgnoreCase`; `Dictionary.Add` throws on a null or duplicate key and `Deserializer`'s child-element loop catches per element into `Errors`, which `InstanceConfiguration.Load` never reads |
| No `-name` switch hosts `Dataphor` | `HostCommandLine.Parse`, `Engine.DefaultServerName` |
| Dataphoria creates `LocalInstance` on 8062 when the list is empty | `Dataphoria.EnsureServerConnection`, `ServerConfiguration.DefaultLocalInstance` |
| `portnumber` 8061 in a created entry, 0 when the attribute is absent | `ServerConfiguration.DefaultPortNumber`; an absent attribute leaves the field at 0 |
| `shouldlisten` defaults to `True` and decides only whether *this* server hosts the listener | field initialiser `_shouldListen = true`; `DataphorServiceHost.Start` |
| `overridelistenerportnumber` 0 means 8060 | `ListenerServiceHost` constructor, `DataphorServiceUtility.DefaultListenerPortNumber` |
| `librarydirectories`: installation `Libraries` in a created entry; binaries' `Libraries` when empty; semicolon-separated; relative entries resolved against the binaries' folder; missing directories created | `ServerConfiguration.DefaultInstance` (`PathUtility.GetInstallationDirectory`), `Server.LibraryDirectory` setter, `Server.GetDefaultLibraryDirectory` (`PathUtility.GetBinDirectory`) |
| `instancedirectory` empty means `<instances folder>\<name>`; relative resolved against the instances folder; holds `Catalog\`, `Log\`, `Data\`, `LibraryData\` | `Server.InstanceDirectory`, `DefaultCatalogDirectory`, `DefaultLogDirectory`, `LibraryUtility.GetInstanceLibraryDirectory` (`LibraryData`), `SimpleDevice.DataDirectoryName` (`Data`) |
| `catalogstoreclassname` empty means the SQLite store; an unloadable name stops the start with 115107, a SQL Server Compact name with 115108 | `Server.DefaultCatalogStoreClassName`, `Server.GetCatalogStoreClassName`, `CatalogException.Codes.CatalogStoreClassNotFound` / `SQLCEStoreNotSupported` (whose message text matches the remedy the section gives) |
| `catalogstoreconnectionstring` empty means `Data Source=<instance>\Catalog\DAECatalog.sqlite`; `%CatalogPath%` is replaced with the catalog folder | `Server.GetCatalogStoreConnectionString`, `GetCatalogStoreDatabaseFileName`, `GetCatalogDirectory` |
| Device settings serialize as `<devicesettings.devicesetting devicename= settingname= settingvalue= xmlns="Alphora.Dataphor.DAE.Schema,Alphora.Dataphor.DAE" />` inside the instance element, under a root `xmlns` of `Alphora.Dataphor.DAE.Server,AlphoraDataphorServer` | `Serializer.GetElementNamespace` with `AssemblyNameUtility.GetName`; the Server assembly is named `AlphoraDataphorServer` (`src/engine/Server/Server.csproj`), and the same root element appears in `InstanceConfigurationTest`'s fixture |
| Device settings applied as the device starts, device name matched exactly; a failure logged as 113248 and the device starts anyway | `Schema.Device.ApplyDeviceSettings`, `DeviceSettings.GetSettingsForDevice` (`==`, so ordinal), `SchemaException.Codes.ErrorApplyingDeviceSetting` |
| A property equal to its `[DefaultValue]` is not written on save | `Serializer.ShouldPersist` returns false and `WriteValue` writes nothing; the four settings carrying the attribute are exactly `shouldlisten`, `overridelistenerportnumber`, `requiresecureconnection`, `requiresecurelistenerconnection` |
| The two security settings are stored and read but nothing acts on them | the only readers are `EditInstanceForm` and the `requireSecureConnection` parameter of `ListenerServiceHost`, whose doc comment says it is not acted on |

**Where the section departs from the plan ticket.** The plan's table said `ShouldListen` "[t]urns off this instance's registration with the machine's listener, so clients must name its port rather than discover it". That is not what the code does, and the section says so instead: the listener answers instance lookups by loading the instance list, so every entry in the file is discoverable through whichever process hosts the listener, and `ShouldListen` decides only whether *this* server tries to host it. The review confirmed this against `ListenerService.GetConfiguration` and `DataphorServiceHost.Start` and let the section stand.

## Review findings

**Read first:** the implement diff (`ticket(implement): host-instance-configuration`) was read before the handoff summary, and every factual claim in the new section was re-derived from the code sites in the table above rather than from the handoff's own verification table.

**Minor, fixed in this pass (six).** All six were wrong statements in prose, each fixed where it stood.

- **`DATAPHOR_INSTANCE_DIRECTORY` was described as leaving machine state alone.** The paragraph ended "That is how a server is run for testing, or a configuration tried out, without changing machine state." Two things the variable does not move, and an operator following that sentence would be bitten by both: the library directory is not redirected (`ServerConfiguration.DefaultInstance` still points a created entry at the installation's `Libraries` folder, and a running server writes into whichever library directory it is given), and a redirected server that takes the computer's listener port answers out of its own private list for every client on the computer. The overclaim is cut and a paragraph names both, with the remedy for each. The listener half is already recorded for developers in `docs/migration-notes.md`; this is its operator-facing statement. (`docs/architecture.md` carries the same "without touching machine state" phrasing in a developer paragraph, left as it is — it is about the instance list, which is what it says.)
- **"Three things write the file" — there are four.** The Dataphoria IDE also writes it, creating `LocalInstance` when the list is empty (`src/windows/Dataphoria/Dataphoria.cs`, `InstanceManager.SaveConfiguration`). The same section's Name paragraph already described that behaviour, so the list contradicted the text two paragraphs below it. Corrected to four, naming the IDE.
- **The `portnumber` table cell said 8061 "in an entry a Dataphor program creates".** The IDE's `LocalInstance` is 8062, as the Name paragraph says. Narrowed to "an entry the server or the Service Configuration Utility creates", matching the setting's own paragraph.
- **The security-settings paragraph credited the Service Configuration Utility with offering them.** It does not: nothing in `src/windows/ConfigurationUtility/` mentions either setting; only `EditInstanceForm` reads and writes them. The Configuration Utility is dropped from that sentence.
- **`docs/architecture.md`'s on-disk table named the wrong per-instance folder.** It said `Instances\<name>\Libraries\` held per-instance library data and `SimpleDevice` tables. The code writes `LibraryData\<library>\` (`LibraryUtility.GetInstanceLibraryDirectory`, via `Server.DefaultLibraryDataDirectory`) and a `SimpleDevice` writes `Data\` (`SimpleDevice.DataDirectoryName`). The handoff spotted this and left it on the grounds that the ticket said to change nothing else in that file; it is a one-row fix in a file this change already touched, and leaving it would have had the architecture document contradict the new section, which names both folders correctly. Fixed, with `Data\` added as its own path in the row.
- **The `[DefaultValue]` `NOTE:` said "this property and the three below"** while sitting above a private field rather than a property. Reworded to "the four properties below", which is what it means and what the code shows.

**Added in this pass: the plan row.** `docs/modernization-plan.md`'s WS5 row for `host-instance-configuration` still read as outstanding work ("what is left is the administrator documentation…"), as did the *Generic host* bullet's closing sentence. Every other landed WS5 ticket's row carries a **Done** clause; the implement pass did not touch the plan at all, which is the anchor document for this ticket. Both now say what landed and link the section.

**Major findings: none.** Stated with its reason, not as a pass: the diff changes no behaviour (documentation plus one comment), so there is no error path, resource, lifetime or type-safety surface in it to get wrong. What a documentation change can get wrong is facts, and the six above are what re-deriving every claim turned up. Nothing in the finding set resolved at a common code site, so there was no class-level invariant to climb to and nothing met the filing bar.

**Conditional/speculative: two, both parked as tripwires, no ticket.** See *Tripwires*.

**Considered and not filed.**

- **Port 0.** The section says an entry written by hand without `portnumber` reads as 0 and tells the operator always to give one; neither the implementer nor the review started a server on port 0 to see what happens. `DataphorServiceHost.BuildHost` passes the number straight to `NetTcpServiceHost.Build` and on to Kestrel's `Listen` with no guard, so a port-0 server would plausibly bind an operating-system-assigned port while its registered endpoint address and the listener's answer both say `:0`. That is inference about an operator-caused misconfiguration, not an observed defect, so it is a `NOTE:` at the site rather than a ticket; a `bug-` ticket would be filed on a `static` repro with the stronger claim unmeasured, which the filing rules forbid.
- **The save-side test** for what a save does to a file an earlier release wrote. `protocol-corewcf-host-prepare` already weighed one and declined it, on the grounds that it would pin BOP rather than anything the product changed. Nothing about that reasoning has changed; not re-filed.
- **`ui-config-utility`'s field and screenshot mismatch.** The Configure form still carries the 3.1 fields and the screenshots still show them. The replacement sentence says the form edits the instance's entry and sends the reader to the settings; it claims nothing about which fields the form has, so it will not need rewriting when that ticket runs.
- **`protocol-transport-security`.** The section states plainly that the two security settings have no effect today. That ticket already commits to amending this section either way.
- **The "Installing Multiple Instances" pointer sentence**, which the implementer added outside the ticket's task list and offered to cut. Kept: that bullet is where a reader configuring a second instance meets the file, it is one sentence, and the alternative is a reader who is told to edit the file and not told where its settings are documented.

**Source hygiene.** Checked after every edit: `docs/users-guide/dataphor-service.md` (218 lines, 218 CRLF, no byte-order mark), `docs/architecture.md` (133/133, none), `docs/modernization-plan.md` (836/836, none), `src/engine/Server/Server/ServerConfiguration.cs` and `src/engine/Server.Hosting/DataphorServiceHost.cs` (307/307, byte-order mark kept, tabs). Every edit was applied by a script that asserts its target text occurs exactly once. No file size concern: the service chapter is 218 lines and the two code files gained comments only. The new section's comments and prose say why and what a reader cannot infer; no narrated statements were found to extract.

## Tests

**None added, none cut.** No behaviour changed in either pass — the only code in the diff is three `NOTE:` comments — so there is nothing for a test to pin and no reproduction owed. The implementer added none, so there were none to cut.

The one load-side claim the section makes about an upgraded file — a file carrying settings the server no longer has is read, and nothing else in it is lost — is pinned by `LoadReadsAFileCarryingSettingsThatNoLongerExist` in `tests/DAE.Server.Tests/InstanceConfigurationTest.cs`, which predates this ticket and which the upgrade paragraph was written against. That fixture's root element also independently corroborates the example's `xmlns` declarations. The save-side claim's test was declined upstream, above.

## Tripwires

Two, both at their code sites, both greppable as `NOTE:`.

- `src/engine/Server/Server/ServerConfiguration.cs`, above the `[DefaultValue]` cluster: those four attributes decide what a saved instance list contains, and the user-guide section documents it, so adding or removing one needs `docs/users-guide/dataphor-service.md` amended in the same change. The guide now has a paragraph that goes stale silently otherwise.
- `src/engine/Server.Hosting/DataphorServiceHost.cs`, at `BuildHost` (added in review): the instance's port number reaches Kestrel unchecked, a hand-written or 3.x instance list can hold `portnumber="0"`, and a server on an ephemeral port would still be advertised as port 0. If an upgraded instance ever shows up as started but unreachable, check the port here and refuse a configuration that cannot be served.

## Validation

- `python tools/docs-link-check/check_links.py` — 1015 links checked, **203 errors, unchanged** from the start of the ticket, and **no error in any file this change touched** (verified by grepping the output for `dataphor-service`, `dataphor-server`, `architecture` and `modernization-plan`). The 203 are the pre-existing missing-reference-page failures under `docs/reference/system-library/` and `docs/reference/frontend-nodes/`.
- `dotnet build Dataphor.slnx` — **0 errors** before and after each edit. 143 warnings, all pre-existing: `CA1416`/`CS0618`/`CA2022` in the Connection and SimpleDevice libraries and `CS0618` in the test project, which is the set `AGENTS.md` records. (The implement handoff reported 26 for the same tree; that was an incremental build that recompiled fewer projects. Nothing changed between the two counts but how much was rebuilt.)
- `dotnet test Dataphor.slnx --no-build -- "NUnit.Where=class != …CoverageScripts"` — **430 passed, 0 failed, 0 skipped** (417 in `DAE.Server.Tests` in 3 m 36 s, 13 in `Common.Tests`). The coverage-script parts were not run: nothing in the diff but comments and Markdown can reach a D4 script, and a comment-only change cannot alter a plan. No pre-existing failure was met.

## Deliberately weaker than it could be

Recorded so a later pass does not mistake caution for an oversight.

- **Port 0** — the section says only that 0 "is not a port a client can be directed to", not what a server started on it does. Making the claim stronger needs a run; the tripwire above says where to look.
- **`DATAPHOR_INSTANCE_DIRECTORY`** — the section describes the redirection as `InstanceManager.ResolveInstanceDirectory` implements it; `host-instance-directory` owns and tests the redirection itself.
- **Error 113248** for a device setting that cannot be applied, and the two catalog-store codes — read from the code and from the exception message text, not reproduced.

No `docs/migration-notes.md` entry: nothing in either pass is a surface an application, a deployment script or a custom C# extension can notice. The three `NOTE:` comments change nothing an extension can see.
