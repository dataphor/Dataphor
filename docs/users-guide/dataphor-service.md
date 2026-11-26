# Dataphor Service

## Summary

The Dataphor Service allows the Dataphor Server to be run as a Microsoft Windows Service. When hosted as a service, the Dataphor Server can be started and stopped using the Services Manager in the Microsoft Windows environment, or by using the Service Configuration Utility. This utility also allows the various settings of the Dataphor Server to be configured for the service.

This chapter is intended mainly for Dataphor administrators, but the material would be helpful for developers as well. The discussion assumes that the material covered in the first chapter is understood.

## Starting the Dataphor Service

The Dataphor Service can be started from within the Windows service configuration manager snap-in, or from the Dataphor Service Configuration Utility. Information about errors that occur while starting the Dataphor Server appear in the "System" or "Dataphor" event logs. These logs can be viewed using the Event Viewer in the Microsoft Windows environment. This application is available on the Administrative Tools sub-menu of the Start menu in Microsoft Windows.

## Manual Installation

The Dataphor Service is installed automatically by the Dataphor installer. However, it can also be installed manually by executing `DAEService.exe` with a parameter of `-i` as follows:

```text
DAEService.exe -i
```

Once the service is installed, it will appear in the list of items visible from the Windows service configuration snap-in in Windows. The service can then be managed like any other Microsoft Windows Service, or using the Dataphor Service Configuration Utility.

## Installing Multiple Instances

One computer can run several Dataphor Server instances side by side, from one set of binaries. Each instance has its own entry in the machine instance list and needs its own port number, catalog directory and library directory. To add one:

- From an elevated command prompt, in the directory holding the Dataphor binaries, register a service for the new instance:

  ```text
  DAEService.exe -i -name "Instance 2"
  ```

  This registers an additional Windows service, named `Alphora Dataphor (Instance 2)`, whose command line names the instance.

- Configure the instance. Use the Service Configuration Utility, or edit the instance's entry in `ServerConfigurations.config`, in the `Alphora\Dataphor\4\Instances` folder under the all-users application data directory (the folder carries the major version, so an installation of an earlier major version on the same machine has its own and the two do not interfere). The port number must be unique on the machine, and the catalog and library directories must not be shared with another instance; the library directory of an existing instance can be copied for the new one. Every setting in that file is described under [Instance Configuration](#instance-configuration).

- Start the service. The instance can also be run in a console window first, without starting the service, by running the following in the directory holding the binaries:

  ```text
  DAEService.exe -run -name "Instance 2"
  ```

  Press Ctrl+C to stop it. `DAEServer.exe` is the same program under another name and takes the same switches.

## Instance Configuration

A computer's Dataphor Server instances are listed in one file, `ServerConfigurations.config`. It is an XML file with one entry per instance, carrying that instance's settings as attributes. The Service Configuration Utility edits it, the Dataphor Server reads it when an instance starts, and it can be edited by hand with any text editor.

### Where the file is

The file lives in the `Alphora\Dataphor\4\Instances` folder under the all-users application data directory, normally:

```text
%ProgramData%\Alphora\Dataphor\4\Instances\ServerConfigurations.config
```

There is one such file per computer, shared by every instance on it. The folder name carries the major version, so an installation of an earlier major version on the same computer keeps its own list and the two do not interfere. The folder is created the first time a Dataphor program looks for the list; the file itself appears the first time something saves it.

A single process can be pointed at a different folder by setting the environment variable `DATAPHOR_INSTANCE_DIRECTORY` before it starts. The variable names the folder that holds both the instance list and — unless the instance overrides it — each instance's own data, so a process started with it set reads a list of its own and writes its catalog, logs and device data below that folder, leaving the computer's own list and data untouched. That is how a server is run for testing, or a configuration tried out. The variable affects only the processes that have it set: a server, the Service Configuration Utility and any client that is to see the same list must all be started with the same value.

Two things the variable does not move. The first is the library directory: an entry the server creates in a redirected list is still pointed at the installation's `Libraries` folder, and a running server writes into whichever library directory it is given, so a server meant to leave the installation alone has to be given a library directory of its own as well. The second is the listener: a redirected server that hosts the listener answers out of the list it read itself, so if it takes the computer's listener port it offers its own private instances to every client on the computer. Give such a server `shouldlisten` of `False`, or start it only while the computer's own listener is already running.

### When the file is read and written

An instance's settings are read when that instance starts, and not again. A change to the file takes effect the next time the instance is restarted.

Four things write the file: the Service Configuration Utility, when a configuration is accepted; the instance editor reached from a client's connection-alias dialog; the Dataphoria IDE, when it creates a local instance to work against; and the Dataphor Server itself. A server started for an instance that has no entry in the list **adds one with default settings and saves the file**. That is what makes a first start produce a working instance with no configuration at all. It is also why a mistyped instance name leaves behind an unwanted entry: the server does not report an unknown instance, it creates one.

The listener service — the service that tells a client which port a named instance is on — answers out of this file and not out of the set of running servers. An entry is therefore visible to a client browsing the computer's instances whether or not a server for it is running, and the port the client is given is the port recorded in the entry.

### The settings

Attribute names in the file are all lower case. The defaults below are what the server uses when a setting is absent or empty; an entry that the server or the Service Configuration Utility creates is written with the values noted as such.

| Setting | Default | What it sets |
|----|----|----|
| `name` | none; required | The instance's name |
| `portnumber` | 8061 in an entry the server or the Service Configuration Utility creates; 0 when absent | The TCP port the instance serves clients on |
| `shouldlisten` | `True` | Whether this server also hosts the computer's listener |
| `overridelistenerportnumber` | `0`, meaning the standard listener port 8060 | The port the listener is hosted on |
| `librarydirectories` | the installation's `Libraries` folder in an entry a Dataphor program creates; the `Libraries` folder beside the binaries when empty | Where the instance looks for libraries |
| `instancedirectory` | empty, meaning a folder named after the instance beside the instance list | Where the instance writes its own data |
| `catalogstoreclassname` | empty, meaning the SQLite store | Which store holds the instance's catalog; the SQL Server store is the one alternative that ships |
| `catalogstoreconnectionstring` | empty, meaning `DAECatalog.sqlite` in the instance's `Catalog` folder | How the catalog store connects |
| `devicesettings` | none | Per-instance device properties; see below |
| `requiresecureconnection`, `requiresecurelistenerconnection` | `False` | Nothing; stored but not acted on, and reported in the instance's log when set |

**Name** (`name`). What identifies the instance: to the `-name` switch on the server's command line, to a client's connection alias, and to the listener. Names are matched without regard to letter case and must be unique within the file. An entry with no name, or with a name an earlier entry already used, is ignored when the list is read, and so is everything else in that entry. A server started with no `-name` switch hosts the instance named `Dataphor`. The Dataphoria IDE, started on a computer whose list is empty, creates an instance named `LocalInstance` on port 8062 and runs it inside its own process.

**Port number** (`portnumber`). The TCP port the instance listens on for client connections. An entry created by the server or the Service Configuration Utility is given 8061. Because several instances can run on one computer, each needs a port of its own, so a second instance has to be given one. The setting has no default of its own: an entry written by hand without it is read as port 0, which is not a port a client can be directed to. Always give one.

**Should listen** (`shouldlisten`). Whether this server, as it starts, also hosts the computer's listener service. `True` unless set otherwise. One listener serves every instance on the computer, which is why its port is fixed, so only the first server to start gets it; the others record in their logs that the port was already in use, which is normal and harmless. Setting this to `False` keeps an instance from ever hosting the listener — useful where a particular instance must not open the shared port — but it does not hide that instance, because the listener answers out of the instance list and so reports every instance in the file. On a computer where no process hosts the listener, clients cannot look instances up by name and must be configured with the instance's host and port directly.

**Override listener port number** (`overridelistenerportnumber`). The port the listener is hosted on when this instance hosts it. `0`, the default, means the standard listener port, 8060. Change it only where 8060 cannot be used, and then change it for every instance and every client on the computer: there is one listener per computer, and a client looking up an instance by name looks on the port it was configured with.

**Library directories** (`librarydirectories`). The folder — or folders, separated by semicolons — the instance searches for available libraries; each sub-folder holding a library descriptor counts as an available library. An entry created by the server or the Service Configuration Utility is given the `Libraries` folder of the installation. When the setting is empty, the instance uses the `Libraries` folder beside its own binaries. An entry that is not a full path is taken as relative to the folder the binaries are in. A running server **writes into its library directory**: it creates folders there and can rewrite library descriptors. A directory that does not exist is created rather than reported, so a mistyped path produces a new empty folder instead of an error, and two instances must never be given the same directory.

**Instance directory** (`instancedirectory`). The folder the instance writes everything of its own into: `Catalog\` for the catalog store file, `Log\` for its logs, and `Data\` and `LibraryData\` for the data its devices persist. Empty, the default, means a folder named after the instance inside the folder that holds the instance list — normally `%ProgramData%\Alphora\Dataphor\4\Instances\<instance name>`. A value that is not a full path is taken as relative to that same folder. Give a full path to put an instance's data somewhere else, for example on another volume.

**Catalog store class name** (`catalogstoreclassname`). Which store keeps the instance's own catalog. Empty, the default, means the SQLite store that ships with Dataphor, and that is what almost every instance should have. A value here is an assembly-qualified .NET class name; a name the server cannot load stops the instance from starting, with error 115107 naming the class. A name belonging to the SQL Server Compact store of earlier releases stops the start with error 115108: that store is no longer supported. The remedy is to clear this setting and the connection string, so that the instance uses the SQLite store, and rebuild the catalog from the application's own scripts.

One store other than SQLite ships, for a deployment that must keep its catalog in a SQL Server database rather than in a file:

```xml
<serverconfiguration name="Reporting" portnumber="8071"
	catalogstoreclassname="Alphora.Dataphor.DAE.Store.MSSQL.MSSQLStore,Alphora.Dataphor.DAE.MSSQL"
	catalogstoreconnectionstring="Data Source=SQLHOST;Initial Catalog=DataphorReporting;Integrated Security=SSPI" />
```

The database named in the connection string is created by the server if it is not there, so the account the server runs as needs the right to create it, or the database should be created in advance. Three things to know about this store. It always sets `MultipleActiveResultSets=True` on its connections, whatever the connection string says, because the catalog reads one of its tables while a read of another is still open and SQL Server refuses that otherwise; leave the keyword out of the connection string. Its text columns are written as Unicode, so non-ASCII characters in catalog object, library or user names are safe; a catalog written by a build before release 4.0 wrote them through `varchar` parameters, which stored a question mark in place of every character the database's code page had no room for, and nothing converts those question marks back. And it adds `Encrypt=False` when the connection string mentions neither `Encrypt` nor `TrustServerCertificate`, because the SQL Server client this release uses would otherwise encrypt the connection and then refuse a server whose certificate this machine does not trust - which a connection string written for release 3 never had to say anything about. To encrypt the catalog connection instead, say so and the default gives way: add `Encrypt=True` for a server whose certificate is trusted, or `Encrypt=True;TrustServerCertificate=True` to encrypt without checking it. SQLite remains the recommendation for every deployment that does not specifically need the catalog in SQL Server. Which SQL Server versions this store and the SQL Server device have been measured against is below, under [Which SQL Server versions work](#which-sql-server-versions-work).

**Catalog store connection string** (`catalogstoreconnectionstring`). How the catalog store connects. Empty, the default, means the catalog is kept in a file named `DAECatalog.sqlite` in the `Catalog` folder of the instance directory. In a value given here, the token `%CatalogPath%` is replaced with that `Catalog` folder, which is the way to vary this setting without writing an absolute path into it: `Data Source=%CatalogPath%\DAECatalog.sqlite` is the default spelled out. Leave it empty unless the catalog file has to live somewhere other than its default place, or a store other than SQLite is in use.

**Require secure connection**, **require secure listener connection** (`requiresecureconnection`, `requiresecurelistenerconnection`). `False` unless set otherwise. These two settings are stored, read, and offered by the instance editor, but **nothing in the server acts on them**: a connection is not secured any differently when they are `True`. Because a setting that silently does nothing is worse than no setting at all, a server that finds either of them set writes a warning to its own log as it starts, naming the settings that are set. The server starts regardless, and its connections are not encrypted. An operator who sees that warning has two choices: remove the settings, which changes nothing about how the instance runs and stops the warning; or, where the connections really must be encrypted, have the server hosted by a program of your own that supplies a binding with transport security of its own (`DataphorServiceHostHooks.BuildBindingCall`, with the matching `DataphorServiceUtility.BuildBindingCall` in every client). That is the only way to encrypt a Dataphor connection in this release; a release that offers it as a setting of its own is planned.

### Device settings

A device is the object that maps a library's tables onto an external database server or onto memory, and the connection details it needs usually differ between a production instance and a test one. Instead of editing the application's D4, those details can be given per instance as device settings.

Each setting names a device, one property of that device, and the value to give it. Settings are written as elements inside the instance's own element:

```xml
<serverconfiguration name="Reporting" portnumber="8071">
	<devicesettings.devicesetting devicename="MyLibrary.MyDevice" settingname="DatabaseName" settingvalue="ReportingCopy" xmlns="Alphora.Dataphor.DAE.Schema,Alphora.Dataphor.DAE" />
</serverconfiguration>
```

A device's settings are applied as that device starts. The device name has to be spelled as the catalog spells it, library qualifier included, and is matched exactly, letter case and all; the property is one of the device's own published properties, which differ from one kind of device to another — a SQL device, for instance, takes `ServerName` and `DatabaseName`. A setting that cannot be applied, because the property is unknown or will not take the value, is written to the instance's log as error 113248 and the device starts anyway: a mistyped setting shows up as a device behaving as though the setting were absent, not as a failure to start. A setting naming a device the instance never starts is never applied.

### An example

```xml
<?xml version="1.0" encoding="utf-8"?>
<instanceconfiguration xmlns:bop="www.alphora.com/schemas/bop" xmlns="Alphora.Dataphor.DAE.Server,AlphoraDataphorServer">
	<serverconfiguration catalogstoreclassname="" catalogstoreconnectionstring="" instancedirectory="" librarydirectories="C:\Program Files\Alphora\Dataphor\Libraries" name="Dataphor" portnumber="8061" />
	<serverconfiguration catalogstoreclassname="" catalogstoreconnectionstring="" instancedirectory="D:\Dataphor\Reporting" librarydirectories="D:\Dataphor\Reporting\Libraries" name="Reporting" portnumber="8071" shouldlisten="False">
		<devicesettings.devicesetting devicename="MyLibrary.MyDevice" settingname="DatabaseName" settingvalue="ReportingCopy" xmlns="Alphora.Dataphor.DAE.Schema,Alphora.Dataphor.DAE" />
	</serverconfiguration>
</instanceconfiguration>
```

This is the shape the writers produce and the shape a hand edit should keep: one `serverconfiguration` element per instance, attribute names in lower case, and the `xmlns` declarations left as they are. A setting whose value is the default for it may be missing altogether, as `shouldlisten` is from the first entry above.

### Upgrading from a file an earlier release wrote

An instance list written by an earlier release is read as it stands; there is no conversion step. Two things are worth knowing about it.

A setting the current server no longer has is ignored, and every other setting on that instance is still read — the file is not rejected and nothing else in it is lost. A file written by release 3.1 is likely to carry two such settings, `useserviceconfiguration` and `allowsilverlightclients`, neither of which exists any more.

Such a setting is then gone from the file the next time anything saves it, because a save writes out the settings the server has, not the ones it read. The same save also drops any setting whose value is the default for it — `shouldlisten`, `overridelistenerportnumber`, `requiresecureconnection` and `requiresecurelistenerconnection` are written only when they differ from the defaults above — and drops any entry that could not be read, such as one whose name duplicated another's. Keep a copy of the old file if its contents matter.

A file naming the SQL Server Compact catalog store of an earlier release will not start; see the catalog store settings above. Catalogs are rebuilt from the application's own scripts rather than migrated.

## Which SQL Server versions work

Both of the places Dataphor talks to a SQL Server — the catalog store described above, and the SQL Server **device** an application's libraries use to hold their tables — go through the same connection code, so one answer covers both.

**SQL Server 2022 is the supported minimum.** SQL Server 2019 also works and needs no settings of its own beyond the connection settings below. Older releases are not supported, although nothing in Dataphor refuses them and SQL Server 2016 passes the same tests today; a deployment on one is on its own if a later change breaks it. Support follows roughly the last five years of SQL Server releases, so a version will leave this list as it ages out.

### What was measured, and on what

Each row below was measured by pointing the test suite's live SQL Server passes at that server: the three transaction-atomicity scripts that commit and roll back across a device boundary, the fifteen catalog-store contract cases, the six text-parameter cases that drive both of the ways a parameter is described to the server, the three cases that read the server's version and reconcile a table the device did not create, and the device-coverage pass, which creates a test library's tables and its 1,180 rows of test data on the server through the device and then stops at a statement in the test script itself that cannot succeed on any version — so it shows that the device works and does not yet check the 1,180 assertions. Measured 26 November 2026; the whole set takes about a minute per server.

| SQL Server | How it was hosted | Version reported | Database collation | Result |
|---|---|---|---|---|
| 2022 | Linux container, SQL login | 16.0.4325.4, Developer Edition | `SQL_Latin1_General_CP1_CI_AS` | **Passes.** Every case passed, and the device-coverage pass stopped where it does on every version |
| 2019 | Linux container, SQL login | 15.0.4510.2, Developer Edition | `SQL_Latin1_General_CP1_CI_AS` | **Passes**, with no extra code or settings |
| 2019 | Windows, LocalDB, integrated security | 15.0.4382.1, Express Edition | `SQL_Latin1_General_CP1_CI_AS` | **Passes** |
| 2016 | Windows, LocalDB, integrated security | 13.0.4001.0, Express Edition | `SQL_Latin1_General_CP1_CI_AS` | Passes, but below the supported minimum — information, not a promise |
| 2025 | not reached | — | — | **Not measured.** The 2025 Linux container image would not start on the machine available for this measurement: the server process stops with a fatal error before it accepts any connection, on both the Ubuntu 22.04 and 24.04 images and whether run as root or not, so nothing about Dataphor was learned either way |
| 2017 | not reached | — | — | **Not measured.** An instance is installed on the measurement machine but starting its Windows service needs administrator rights the measurement did not have. It sits between two versions that pass |

Every row that was reached reports the same database collation, so **a pass above is a pass on `SQL_Latin1_General_CP1_CI_AS` and not on every collation.** The collation decides what a `varchar` column can hold: a character the collation's code page has no room for is stored as a question mark, silently, which is why Dataphor writes text through Unicode parameters. A deployment on a different collation should run the same passes against its own server before trusting the table above.

Three things the measurement confirmed on every version it reached, each of which is the first thing to fail against a server nobody has tried: the device reads the server's version correctly from `serverproperty('productversion')` and does not mistake any of these for SQL Server 7.0, whose dialect it still knows how to emit; the device creates the database named in its definition when that database is absent; and the server-side cursor path — the undocumented `sp_cursoropen` and `sp_cursorfetch` procedures the device uses for every read whose statement begins with `select` — works. A failure in that last one would be the strongest argument for making the device use client-side cursors instead, and there was none.

### Connection settings

The SQL Server client this release uses encrypts by default and then checks the server's certificate, which earlier releases did not. An internal SQL Server usually has a self-signed certificate that the machines connecting to it do not trust, so this is the setting a deployment most often has to add. The device and the catalog store each add `Encrypt=False` for themselves — which is what release 3.1 did — so a device definition or a catalog store connection string carried over from release 3.1 keeps connecting. Anything that connects with the exact text it is given does not.

Measured against SQL Server 2019 and 2022 with a self-signed certificate the client did not trust:

| In the connection string | 2019 | 2022 | What it means |
|---|---|---|---|
| nothing about encryption | fails: *the certificate chain was issued by an authority that is not trusted* | fails, same way | the client encrypts and checks the certificate |
| `Encrypt=False` | connects | connects | unencrypted; what release 3.1 did, and what the device and the catalog store default to |
| `TrustServerCertificate=True` | connects | connects | encrypted, certificate not checked |
| `Encrypt=True;TrustServerCertificate=True` | connects | connects | the same, said explicitly |
| `Encrypt=Strict` | fails before login | fails before login | strict encryption ignores `TrustServerCertificate`, so it needs a certificate the machine really trusts; and 2019 does not support it at all |

A device whose definition names only a server and a database connects to such a server unchanged, through its own `Encrypt=False` — measured against SQL Server 2022. To encrypt a device's connection instead, give the device `ConnectionParameters`, which is applied after that default and overrides it: `"ConnectionParameters" = "Encrypt=True"` for a server whose certificate is trusted, or `"Encrypt=True;TrustServerCertificate=True"` to encrypt without checking it. The catalog store takes the same settings in its connection string, as described under the catalog store above.

### Adopting a schema the device did not create

A device in a reconciliation mode whose master is the device reads the tables already in its database and creates a D4 table variable for each. Three things about that are worth knowing before pointing a device at an existing schema, and all three are the same on every version above.

The D4 name includes the SQL schema: a `dbo` table called `Invoices` becomes `MyLibrary.dbo.Invoices`, not `MyLibrary.Invoices`, unless the device's `Schema` attribute is set to `dbo`, in which case the schema is left out of the name.

Columns of type `datetime2` and `datetimeoffset` are **left out of the reconciled table variable, with nothing said about it.** The device maps `datetime`, `date` and `time`, but not the two types SQL Server added alongside them in 2008, so a table using them reconciles with fewer columns than it has. Check a reconciled table variable's columns against the SQL table before relying on it.

A Unicode column reconciles twice as wide as it is: an `nvarchar(30)` column arrives with a length of 60, because SQL Server reports that column's width in bytes and Dataphor reads the number as a count of characters. The length constraint Dataphor then puts on the column admits strings the server will not store, so the error arrives from SQL Server on the write rather than from Dataphor on the edit. Both of these are defects rather than decisions and are expected to change; a release that fixes them will say so in the migration notes.

## Which PostgreSQL versions work

This section is about the PostgreSQL **device** an application's libraries use to hold their tables. There is no PostgreSQL catalog store in this release — the catalog goes in SQLite or in SQL Server — so unlike the SQL Server answer above, this one covers one thing rather than two.

**PostgreSQL 14 is the supported minimum.** Versions 14, 16 and 18 were measured and behave identically; 13 was measured too and also behaves identically, but it is past the end of PostgreSQL's own support and is therefore not a supported target. Support follows roughly the last five years of PostgreSQL releases, so a version will leave this list as it ages out. Nothing in Dataphor refuses an older server.

### What was measured, and on what

Each row was measured by pointing the test suite's live PostgreSQL pass at that server. It creates a test library's tables on the server through the device, puts the library's 1,180 rows of recorded test data in them, and then runs those 1,180 cases — one per D4 scalar type and per operator — against the device. Measured 26 November 2026, against the official `postgres` container images, each reached over TCP with a PostgreSQL login. The pass takes about 13 seconds per server.

| PostgreSQL | Version reported | Result |
|---|---|---|
| 18 | 18.7 (Debian) | **Passes**, with the failures listed below |
| 16 | 16.16 (Debian) | **Passes**, identically |
| 14 | 14.25 (Debian) | **Passes**, identically |
| 13 | 13.23 (Debian) | Passes identically, but is below the supported minimum — information, not a promise |

"Identically" is the point of the table: the four servers differ only in which members of one unstable family of test cases fail (five to seven of them, and a different subset each run — see below). Everything else — every type that round-trips, every operator that translates, the database the device creates, the support functions it installs — is the same from 13 through 18. So the version floor above is a support decision, not a measurement of where the device stops working.

Three things the measurement confirmed on every version, each of which is the first thing to fail against a server nobody has tried. The device reads the server's version from `version()` and parses it. Starting the device **creates the database** named in its definition when that database is absent, and installs the fifty-seven `DAE_` support functions it needs into it. And the lock the device takes before every insert into one of its tables — a `for update` clause on a key-check select, which PostgreSQL has to accept in a position SQL Server does not use — both parses and takes a real row lock, checked directly against the server.

### What still fails, and what is not yet proved

Of the 1,180 recorded cases, these families fail. None of them is new to PostgreSQL alone: the first three fail the same way against SQL Server and are already recorded as defects.

- **Ordering.** A table held on the device sorts differently from the same rows sorted in the engine, in five to seven of the thirty-four ordering cases, and which ones changes between identical runs. A device sorts by the database's rules; whether a D4 `order` is promised to give the same sequence either way is an open question rather than a settled defect.
- **The current date and time.** A case that stores `DateTime()` (or `Date()`) in a table on the device and then checks the value against a second call to the same operator fails. The device translates those operators to the *database server's* clock, so against a database server in another time zone an application gets that server's wall-clock time, not the Dataphor server's.
- **A table keyed on a `Binary` column** refuses a second, different value as a duplicate.
- **Case-insensitive comparison.** A `UserID` column on the device compares case-*sensitively*, because PostgreSQL compares text by the database collation. D4's `UserID` is case-insensitive.
- **`Time` columns.** A D4 `Time` value can be written to a PostgreSQL device table but cannot be compared with one: the device declares the column `time` while every operator it registers over that column is written for `timestamp`, so a comparison fails on the server. **Avoid `Time` columns on a PostgreSQL device in this release**; a `DateTime` column works.

Two further things are not proved either way. Nothing in the test corpus stores a value large enough to need the device's streamed read path, so large `Binary` and `SQLText` values are untested beyond about 200 bytes. And a `Money` column holds only two decimal places, because that is what PostgreSQL's `money` type holds: a D4 `Money` value with more than two — a unit price, a tax rate — is silently rounded on the way in. Use a `Decimal` column for those.

One behaviour is worth knowing because it has no equivalent on SQL Server: **after any failed statement, a PostgreSQL transaction is unusable until it ends.** Dataphor does not currently notice, so D4 that catches an error from a PostgreSQL device inside a transaction and carries on will find every later statement failing with *current transaction is aborted*. Let the transaction end and start a new one.

### Connection settings

A device definition names the server, the port and the database, and the login goes on a device user rather than on the device, so that it is stored encrypted:

```
create device MyDevice
	reconciliation { mode = { command }, master = device }
	class "PostgreSQLDevice.PostgreSQLDevice"
	attributes
	{
		"Server" = "dbhost",
		"Port" = "5432",
		"Database" = "mydatabase"
	};

CreateDeviceUser('Admin', 'MyDevice', 'myloginname', 'mypassword');
```

`Port` defaults to 5432 and `Server` to 127.0.0.1, so a device on the same machine can leave both out. `Database` defaults to `postgres`, which is almost never what a deployment wants. `SearchPath` sets the server's schema search path for the device's connections and may be left out. The device creates the database if it is absent, which needs a login with the right to create one; give it an existing database and a login without that right if that is not wanted, and set `"ShouldEnsureDatabase" = "False"`.

Nothing about encryption was measured: the servers in the table above accept unencrypted connections, which is what the driver then uses. A server that requires TLS needs the driver's own keywords, which reach it through the device's `ConnectionParameters` attribute — for example `"ConnectionParameters" = "SSL Mode=Require"`. **One keyword the driver does not recognise now fails the connection** where the driver this device used in release 3.1 ignored it, so a `ConnectionParameters` setting carried over from release 3.1 should be checked against [the driver's documented keywords](https://www.npgsql.org/doc/connection-string-parameters.html).

## Uninstalling

The Dataphor Service can be manually uninstalled from the Windows services list by running `DAEService.exe` with the `-u` parameter as follows:

```text
DAEService.exe -u
```

If there is an error during the install or uninstall process, the error message is written to the console and the program exits with a non-zero exit code. Registering and removing a service needs an elevated command prompt.

## Service Configuration Utility

The Service Configuration Utility is a Microsoft .NET application that can be used to start and stop the Dataphor Service, as well as configure the various settings of the Dataphor Server. The application runs as a background process in the system tray. Clicking on this icon will display the main form of the Service Configuration Utility.

### Configuration

The following screen capture displays the main form of the Dataphor Service Configuration Utility:

![Dataphor Service - Configuration Utility - Main Form](../images/dataphor-service-configuration-utility-main-form.png)

To configure the service, click on the Configure button to display the following form:

![Dataphor Service - Configuration Utility - Edit](../images/dataphor-service-configuration-utility-configuration-edit.png)

This form edits the entry for this service's instance in the computer's instance list. Each setting it holds, what it defaults to and when to change it is described under [Instance Configuration](#instance-configuration).

Accepting the configuration by clicking OK saves the configuration information. Note that configuration options cannot be changed while the Dataphor Server is running. The configuration information will be saved, and a message will be displayed indicating that the new configuration will not be used until the Dataphor Server is re-started.

### Other Options

The following table lists the options available for the Dataphor Service:

| Option | Description |
|----|----|
| Auto Start | Determines whether the Dataphor Service will start automatically when Microsoft Windows starts. |
| Show Icon in the System Tray (Options Menu) | Determines whether to display the Dataphor Service Configuration Utility icon in the system tray. |
| Run Configuration Utility at Startup (Options Menu) | Determines whether to start the Dataphor Service Configuration Utility when Windows starts. |

### Troubleshooting

The following table lists some common problems and remedies.

**Dataphor Service Troubleshooting**

<table>
<colgroup>
<col style="width: 50%" />
<col style="width: 50%" />
</colgroup>
<thead>
<tr>
<th style="text-align: left;">Problem</th>
<th style="text-align: left;">Reason/Suggestions</th>
</tr>
</thead>
<tbody>
<tr>
<td style="text-align: left;"><p><img src="../images/unavailable.png" width="48" height="48" alt="image" /> Start, Stop, and Auto Start options are grayed and cannot be selected.</p></td>
<td style="text-align: left;"><p>The Dataphor Service has not been installed.</p>
<p>Install the Dataphor Service, see <a href="#manual-installation">Installing the Dataphor Service</a>.</p></td>
</tr>
<tr>
<td style="text-align: left;"><p><img src="../images/stopped.png" width="48" height="48" alt="image" /> Dataphor clients cannot connect to the server.</p></td>
<td style="text-align: left;"><p>Server is stopped.</p>
<p>Start the server.</p>
<p>If the server is running and clients still cannot connect, see the next item.</p>
<p>If the server cannot be started (request times out) try changing the port setting of the server.</p></td>
</tr>
<tr>
<td style="text-align: left;"><p><img src="../images/running.png" width="48" height="48" alt="image" /> Dataphor clients cannot connect to the server.</p></td>
<td style="text-align: left;"><p>Server configuration does not match connect request from client.</p>
<p>Verify the configuration of the server and that the client requests match the port number and host name of the Dataphor Server.</p></td>
</tr>
</tbody>
</table>
