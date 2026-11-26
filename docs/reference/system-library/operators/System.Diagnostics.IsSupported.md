# System.Diagnostics.IsSupported

**Declarations**

```d4
create operator .System.Diagnostics.IsSupported(const ADeviceName : System.Name, const AStatement : System.String) : System.Boolean
 class "System.SystemIsSupportedNode" static tags { DAE.IsRemotable = "False" };
```

**Remarks**

`IsSupported` returns `true` if `AStatement` is supported by `ADeviceName`, otherwise it returns `falst`.

**Using System.Diagnostics.IsSupported**

```d4
CreateLibrary
(
 LibraryDescriptor
 (
  "Lib",
  VersionNumberUndefined(),
  "Device",
  list(FileReference) {},
  list(LibraryReference)
  {
   LibraryReference("MSSQLDevice",VersionNumberUndefined()),
   LibraryReference("SimpleDevice",VersionNumberUndefined())
  }
 )
);
RegisterLibrary("Lib");
create device Device
 reconciliation { mode = { Command }, master = device }
 class "MSSQLDevice"
        attributes { "ServerName" = ".", "DatabaseName" = "Docs" };
CreateDeviceUser("System", "Device", "sa", "");
CreateDeviceUser("Admin", "Device", "sa", "");
create table SQLTable { ID : Integer, key { ID } };
select
 if IsSupported('Device', 'SQLTable group add { Count() Count }')
 then "MSSQLDevice supports Count"
 else "MSSQLDevice does not support Count";
drop table SQLTable;
drop device Device;
create device Device
 reconciliation { mode = { Command }, master = device }
 class "SimpleDevice"
  attributes
  {
   "DirectoryName" = "c:\Data",
   "AutoSave" = "true"
  };
create table SimpleTable { ID : Integer, key { ID } };
select
 if IsSupported('Device', 'SimpleTable group add { Count() Count }')
 then "SimpleDevice supports Count"
 else "SimpleDevice does not support Count";
drop table SimpleTable;
drop device Device;
UnregisterLibrary("Lib");
DropLibrary("Lib");
```

```text
MSSQLDevice supports Count

SimpleDevice does not support Count
```

**See Also**

[System.Diagnostics.GetDetailedErrorDecsription](System.Diagnostics.GetDetailedErrorDescription.md) \| [System.Diagnostics.LogError](System.Diagnostics.LogError.md) \| [System.Diagnostics.LogMessage](System.Diagnostics.LogMessage.md) \| [System.Error](System.Error.md)
