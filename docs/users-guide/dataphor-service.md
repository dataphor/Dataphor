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
| `catalogstoreclassname` | empty, meaning the SQLite store | Which store holds the instance's catalog |
| `catalogstoreconnectionstring` | empty, meaning `DAECatalog.sqlite` in the instance's `Catalog` folder | How the catalog store connects |
| `devicesettings` | none | Per-instance device properties; see below |
| `requiresecureconnection`, `requiresecurelistenerconnection` | `False` | Nothing; stored but not acted on |

**Name** (`name`). What identifies the instance: to the `-name` switch on the server's command line, to a client's connection alias, and to the listener. Names are matched without regard to letter case and must be unique within the file. An entry with no name, or with a name an earlier entry already used, is ignored when the list is read, and so is everything else in that entry. A server started with no `-name` switch hosts the instance named `Dataphor`. The Dataphoria IDE, started on a computer whose list is empty, creates an instance named `LocalInstance` on port 8062 and runs it inside its own process.

**Port number** (`portnumber`). The TCP port the instance listens on for client connections. An entry created by the server or the Service Configuration Utility is given 8061. Because several instances can run on one computer, each needs a port of its own, so a second instance has to be given one. The setting has no default of its own: an entry written by hand without it is read as port 0, which is not a port a client can be directed to. Always give one.

**Should listen** (`shouldlisten`). Whether this server, as it starts, also hosts the computer's listener service. `True` unless set otherwise. One listener serves every instance on the computer, which is why its port is fixed, so only the first server to start gets it; the others record in their logs that the port was already in use, which is normal and harmless. Setting this to `False` keeps an instance from ever hosting the listener — useful where a particular instance must not open the shared port — but it does not hide that instance, because the listener answers out of the instance list and so reports every instance in the file. On a computer where no process hosts the listener, clients cannot look instances up by name and must be configured with the instance's host and port directly.

**Override listener port number** (`overridelistenerportnumber`). The port the listener is hosted on when this instance hosts it. `0`, the default, means the standard listener port, 8060. Change it only where 8060 cannot be used, and then change it for every instance and every client on the computer: there is one listener per computer, and a client looking up an instance by name looks on the port it was configured with.

**Library directories** (`librarydirectories`). The folder — or folders, separated by semicolons — the instance searches for available libraries; each sub-folder holding a library descriptor counts as an available library. An entry created by the server or the Service Configuration Utility is given the `Libraries` folder of the installation. When the setting is empty, the instance uses the `Libraries` folder beside its own binaries. An entry that is not a full path is taken as relative to the folder the binaries are in. A running server **writes into its library directory**: it creates folders there and can rewrite library descriptors. A directory that does not exist is created rather than reported, so a mistyped path produces a new empty folder instead of an error, and two instances must never be given the same directory.

**Instance directory** (`instancedirectory`). The folder the instance writes everything of its own into: `Catalog\` for the catalog store file, `Log\` for its logs, and `Data\` and `LibraryData\` for the data its devices persist. Empty, the default, means a folder named after the instance inside the folder that holds the instance list — normally `%ProgramData%\Alphora\Dataphor\4\Instances\<instance name>`. A value that is not a full path is taken as relative to that same folder. Give a full path to put an instance's data somewhere else, for example on another volume.

**Catalog store class name** (`catalogstoreclassname`). Which store keeps the instance's own catalog. Empty, the default, means the SQLite store that ships with Dataphor, and that is what almost every instance should have. A value here is an assembly-qualified .NET class name; a name the server cannot load stops the instance from starting, with error 115107 naming the class. A name belonging to the SQL Server Compact store of earlier releases stops the start with error 115108: that store is no longer supported. The remedy is to clear this setting and the connection string, so that the instance uses the SQLite store, and rebuild the catalog from the application's own scripts.

**Catalog store connection string** (`catalogstoreconnectionstring`). How the catalog store connects. Empty, the default, means the catalog is kept in a file named `DAECatalog.sqlite` in the `Catalog` folder of the instance directory. In a value given here, the token `%CatalogPath%` is replaced with that `Catalog` folder, which is the way to vary this setting without writing an absolute path into it: `Data Source=%CatalogPath%\DAECatalog.sqlite` is the default spelled out. Leave it empty unless the catalog file has to live somewhere other than its default place, or a store other than SQLite is in use.

**Require secure connection**, **require secure listener connection** (`requiresecureconnection`, `requiresecurelistenerconnection`). `False` unless set otherwise. These two settings are stored, read, and offered by the instance editor, but **nothing in the server acts on them**: a connection is not secured any differently when they are `True`. They are described here so that an operator who finds them set in a file knows they are having no effect.

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
