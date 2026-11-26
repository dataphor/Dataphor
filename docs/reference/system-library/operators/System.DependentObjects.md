# System.DependentObjects

**Declarations**

```d4
create operator .System.DependentObjects(const AObjectID : System.Integer) : table { Object_ID : System.Integer, Object_Name : System.Name, Object_Description : System.String, Sequence : System.Integer, Level : System.Integer }
 class "System.SystemDependentObjectsNode" static tags { DAE.IsRemotable = "False" };
create operator .System.DependentObjects(const AObjectID : System.Integer, const ARecursive : System.Boolean) : table { Object_ID : System.Integer, Object_Name : System.Name, Object_Description : System.String, Sequence : System.Integer, Level : System.Integer }
 class "System.SystemDependentObjectsNode" static tags { DAE.IsRemotable = "False" };
create operator .System.DependentObjects(const AObjectName : System.Name) : table { Object_ID : System.Integer, Object_Name : System.Name, Object_Description : System.String, Sequence : System.Integer, Level : System.Integer }
 class "System.SystemDependentObjectsNode" static tags { DAE.IsRemotable = "False" };
create operator .System.DependentObjects(const AObjectName : System.Name, const ARecursive : System.Boolean) : table { Object_ID : System.Integer, Object_Name : System.Name, Object_Description : System.String, Sequence : System.Integer, Level : System.Integer }
 class "System.SystemDependentObjectsNode" static tags { DAE.IsRemotable = "False" };
```

**Remarks**

`DependentObjects` returns a table listing the objects that depend on the specified object `AObjectID` or `AObjectName`. If `ARecursive` is `true` (or omitted) ALL objects that depend on the specified object will be listed. This includes any objects that depend on any objects that depend on `AObjectID` or `AObjectName`, and so on. If `ARecursive` is `false` only the objects that directly depend on the specified object itself will be listed.

**Using System.DependentObjects**

```d4
CreateLibrary
(
 LibraryDescriptor
 (
  "Lib",
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
create view View Table where Name = "Alphora";
select DependentObjects("Device", false)
 over { Object_Description, Sequence, Level };
select DependentObjects("Device", true)
 over { Object_Description, Sequence, Level };
UnregisterLibrary("Lib");
DropLibrary("Lib");
```

```d4
Object_Description              Sequence Level
------------------------------- -------- -----
Base table variable "Lib.Table" 1        1
```

```d4
Object_Description                Sequence Level
--------------------------------- -------- -----
Base table variable "Lib.Table"   1        1
Derived table variable "Lib.View" 2        2
```

**See Also**

[Objects](../../../developers-guide/d4-catalog.md#objects) \| [System.RequiredObjects](System.RequiredObjects.md) \| [System.ObjectExists](System.ObjectExists.md) \| [System.ObjectID](System.ObjectID.md) \| [System.ObjectName](System.ObjectName.md)
