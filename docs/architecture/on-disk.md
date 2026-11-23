# On disk

Where a deployment's files live: binaries, libraries, instance configuration, catalog store and client aliases. Part of the [Dataphor architecture](../architecture.md).

| Location | Content |
|---|---|
| `<install>\` | Binaries, with the native SQLite interop library |
| `<install>\Libraries\<Name>\` | Shipped libraries; library assemblies are built *into* these folders |
| `%ProgramData%\Alphora\Dataphor\4\Instances\ServerConfigurations.config` | Instance list (the whole folder is redirected by `DATAPHOR_INSTANCE_DIRECTORY`); every setting in it is documented in [Instance Configuration](../users-guide/dataphor-service.md#instance-configuration) |
| `%ProgramData%\Alphora\Dataphor\4\Instances\<name>\Catalog\` | Catalog store file |
| `%ProgramData%\Alphora\Dataphor\4\Instances\<name>\LibraryData\`, `…\Data\`, `…\Log\` | Per-instance library data (documents device), `SimpleDevice` tables, logs |
| `%ProgramData%` and `%AppData%` `…\Alphora\Dataphor\4\Aliases.config` | Machine and user client aliases; the client has always carried the major version here, and `Instances` is a folder beside it |
