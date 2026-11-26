# SimpleDevice

This chapter provides reference material for working with and understanding the Dataphor SimpleDevice Library.

## Definition

The following information describes the definition of the SimpleDevice library.

**Version**

3.0.0.0

**Requisites**

| Name   | Version |
|--------|---------|
| System | \*      |

**SimpleDevice Files**

| Name                                   | Is Assembly |
|----------------------------------------|-------------|
| Alphora.Dataphor.DAE.Device.Simple.dll | True        |

**See Also**

[SQLDevice](SQLDevice.md) \| [MSSQLDevice](MSSQLDevice.md) \| OracleDevice \| DB2Device

### Registered Assemblies

The following lists the assemblies registered in the SimpleDevice Library.

Alphora.Dataphor.DAE.Device.Simple

## Documentation

Data stored using a memory device will be lost when the Dataphor Server shuts down, but the table definitions will be kept and restored as part of the catalog. This makes it useful for tasks such as development.

The simple device is the memory device with the added ability to persist the data in files on the DataPhor Server local storage media. The Simple Device, while it will persists the data, is intended for small amounts data that does not require concurrent access.

### Settings

**Device Map Item Commalist**

The device map item commalist is valid, but not currently used in the Simple or Memory devices. The Simple and Memory devices only support indexed access to base table variables, but it supports all types.

**Reconciliation Mode Description**

The valid reconciliation modes are:

- `None`

  No reconciliation will be performed.

- `Startup`

  Reconciliation will be performed on device startup.

- `Command`

  Reconciliation will be performed in response to each DDL command.

- `Automatic`

  Reconciliation will be performed when a catalog miss occurs, and this device is the default device.

**Reconciliation Master**

The reconciliation master can be server, device, or both. Server indicates that the DAE contains the master copy of the catalog. Device indicates that the device is considered the master catalog, and both indicates that the catalogs should be reconciled from both directions.

Reconciliation for a device proceeds as follows:

1\. If the reconciliation master setting is server or both, each table in the DAE catalog is reconciled against the device. If a table does not exist in the device, it is created; otherwise, it is updated to match the DAE.

2\. If the reconciliation master setting is device or both, each table in the device catalog is reconciled against the DAE. If it does not exist in the DAE, it is created; otherwise, it is updated to match the device.

**Attributes**

The attributes for the Simple Device are:

- `AutoSave` : `Boolean`

  `true` = every change to the table will be saved immediately. (Default)

  `false` = changes are only saved when `SaveTable` is called.

- `DirectoryName` : `String`

  Specifies a directory for the data files created by this device for storing the data. If the given path is not rooted, it is interpreted as a sub-directory of the run-time directory of the Dataphor Server. If DirectoryName is not specified at all, a directory named `Data` will be created as a sub-directory of the run-time directory of the Dataphor Server.

  The data files are named LibraryName.TableName.dat

  > [!WARNING]
  > Do not modify these files at all. Any changes made to these files outside of Dataphor will render the files unusable and/or the data unreliable.

**See Also**

[Simple Device Operators](SimpleDevice.md#operators) \| [Devices](../../developers-guide/d4-catalog.md#device)

### Example

```d4
create device DataDevice
 reconciliation { mode = { Command }, master = device }
 class "SimpleDevice"
  attributes
  {
   "DirectoryName" = "c:\Data",
   "AutoSave" = "true"
  };
```

## Operators

The following sections describe the operators of the SimpleDevice Library.

### SimpleDevice.BeginUpdate

**Declarations**

```d4
create operator .SimpleDevice.BeginUpdate(const ADeviceName : System.Name, const ATableName : System.Name)
 class "SimpleDevice.BeginUpdateNode" static tags { DAE.IsRemotable = "false" };
```

**Remarks**

`BeginUpdate` is used when the simple device setting AutoSave is true. This allows the AutoSave feature to be suspended from the point the `BeginUpdate` operator is used until the `EndUpdate` operator is used. This permits improved performance when a rapid sequence of updates is made to the tables.

**Using SimpleDevice.BeginUpdate**

```d4
CreateLibrary
 (LibraryDescriptor
  ("Lib",
   VersionNumberUndefined(),
   "Device",
   list(FileReference) {},
   list(LibraryReference) { LibraryReference("SimpleDevice",VersionNumberUndefined()) }
  )
 );
RegisterLibrary("Lib");
create device Device
    reconciliation { mode = { Command }, master = device }
    class "SimpleDevice"
        attributes
        {
            "DirectoryName" = "c:\Data",
            "AutoSave" = "true"
        };
create table Table { ID: Integer, Name : String, key{ID} };
BeginUpdate("Device", "Table");
insert table { row { 1 ID, "Jim" Name } } into Table;
insert table { row { 2 ID, "Jeff" Name } } into Table;
EndUpdate("Device", "Table");
select Table;
TruncateTable("Device", "Table");
drop table Table;
UnregisterLibrary("Lib");
DropLibrary("Lib");
```

```text
ID Name
-- ----
1  Jim
2  Jeff
```

**See Also**

[SimpleDevice.EndUpdate](SimpleDevice.md#simpledeviceendupdate) \| [SimpleDevice.LoadTable](SimpleDevice.md#simpledeviceloadtable) \| [SimpleDevice.SaveTable](SimpleDevice.md#simpledevicesavetable) \| [SimpleDevice.TruncateTable](SimpleDevice.md#simpledevicetruncatetable)

### SimpleDevice.EndUpdate

**Declarations**

```d4
create operator .SimpleDevice.EndUpdate(const ADeviceName : System.Name, const ATableName : System.Name)
 class "SimpleDevice.EndUpdateNode" static tags { DAE.IsRemotable = "false" };
```

**Remarks**

`EndUpdate` is used when the simple device setting AutoSave is true. This allows the AutoSave feature to be suspended from the point the `BeginUpdate` operator is used until the `EndUpdate` operator is used. This permits improved performance when a rapid sequence of updates is made to the tables.

**Using SimpleDevice.EndUpdate**

```text
See the SimpleDevice.BeginUpdate example.
```

**See Also**

[SimpleDevice.BeginUpdate](SimpleDevice.md#simpledevicebeginupdate) \| [SimpleDevice.LoadTable](SimpleDevice.md#simpledeviceloadtable) \| [SimpleDevice.SaveTable](SimpleDevice.md#simpledevicesavetable) \| [SimpleDevice.TruncateTable](SimpleDevice.md#simpledevicetruncatetable)

### SimpleDevice.LoadTable

**Declarations**

```d4
create operator .SimpleDevice.LoadTable(const ADeviceName : System.Name, const ATableName : System.Name)
 class "SimpleDevice.LoadTableNode" static tags { DAE.IsRemotable = "false" };
```

**Remarks**

`LoadTable` loads the table from the file into memory. Loading of the data takes place automatically when the first need for the data is encountered. This operator is used internally by the system and need not be used.

**Using SimpleDevice.LoadTable**

```d4
LoadTable ("Data", "Table")
```

**See Also**

[SimpleDevice.BeginUpdate](SimpleDevice.md#simpledevicebeginupdate) \| [SimpleDevice.EndUpdate](SimpleDevice.md#simpledeviceendupdate) \| [SimpleDevice.SaveTable](SimpleDevice.md#simpledevicesavetable) \| [SimpleDevice.TruncateTable](SimpleDevice.md#simpledevicetruncatetable)

### SimpleDevice.SaveTable

**Declarations**

```d4
create operator .SimpleDevice.SaveTable(const ADeviceName : System.Name, const ATableName : System.Name)
 class "SimpleDevice.SaveTableNode" static tags { DAE.IsRemotable = "false" };
```

**Remarks**

`SaveTable` causes the table to be written from the memory to the file. Use of this operator is necessary when the simple device setting AutoSave is false.

**Using SimpleDevice.SaveTable**

```d4
CreateLibrary
 (LibraryDescriptor
  ("Lib",
   VersionNumberUndefined(),
   "Device",
   list(FileReference) {},
   list(LibraryReference) { LibraryReference("SimpleDevice",VersionNumberUndefined()) }
  )
 );
RegisterLibrary("Lib");
create device Device
    reconciliation { mode = { Command }, master = device }
    class "SimpleDevice"
        attributes
        {
            "DirectoryName" = "c:\Data",
            "AutoSave" = "false"
        };
create table Table { ID: Integer, Name : String, key{ID} };
insert table { row { 1 ID, "Jim" Name } } into Table;
insert table { row { 2 ID, "Jeff" Name } } into Table;
SaveTable("Device", "Table");
select Table;
TruncateTable("Device", "Table");
UnregisterLibrary("Lib");
DropLibrary("Lib");
```

```text
ID Name
-- ----
1  Jim
2  Jeff
```

**See Also**

[SimpleDevice.BeginUpdate](SimpleDevice.md#simpledevicebeginupdate) \| [SimpleDevice.EndUpdate](SimpleDevice.md#simpledeviceendupdate) \| [SimpleDevice.LoadTable](SimpleDevice.md#simpledeviceloadtable) \| [SimpleDevice.TruncateTable](SimpleDevice.md#simpledevicetruncatetable)

### SimpleDevice.TruncateTable

**Declarations**

```d4
create operator .SimpleDevice.TruncateTable(const ADeviceName : System.Name, const ATableName : System.Name)
 class "SimpleDevice.TruncateTableNode" static tags { DAE.IsRemotable = "false" };
```

**Remarks**

`TruncateTable` clears all data from memory and deletes the file for the specified table on the specified device.

**Using SimpleDevice.TruncateTable**

```text
See the SimpleDevice.BeginUpdate example.
```

**See Also**

[SimpleDevice.BeginUpdate](SimpleDevice.md#simpledevicebeginupdate) \| [SimpleDevice.EndUpdate](SimpleDevice.md#simpledeviceendupdate) \| [SimpleDevice.LoadTable](SimpleDevice.md#simpledeviceloadtable) \| [SimpleDevice.SaveTable](SimpleDevice.md#simpledevicesavetable)
