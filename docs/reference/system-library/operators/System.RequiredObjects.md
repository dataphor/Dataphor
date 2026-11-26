# System.RequiredObjects

**Declarations**

```d4
create operator .System.RequiredObjects(const AObjectID : System.Integer) : table { Object_ID : System.Integer, Object_Name : System.Name, Object_Description : System.String, Sequence : System.Integer, Level : System.Integer }
 class "System.SystemRequiredObjectsNode" static tags { DAE.IsRemotable = "False" };
create operator .System.RequiredObjects(const AObjectID : System.Integer, const ARecursive : System.Boolean) : table { Object_ID : System.Integer, Object_Name : System.Name, Object_Description : System.String, Sequence : System.Integer, Level : System.Integer }
 class "System.SystemRequiredObjectsNode" static tags { DAE.IsRemotable = "False" };
create operator .System.RequiredObjects(const AObjectName : System.Name) : table { Object_ID : System.Integer, Object_Name : System.Name, Object_Description : System.String, Sequence : System.Integer, Level : System.Integer }
 class "System.SystemRequiredObjectsNode" static tags { DAE.IsRemotable = "False" };
create operator .System.RequiredObjects(const AObjectName : System.Name, const ARecursive : System.Boolean) : table { Object_ID : System.Integer, Object_Name : System.Name, Object_Description : System.String, Sequence : System.Integer, Level : System.Integer }
 class "System.SystemRequiredObjectsNode" static tags { DAE.IsRemotable = "False" };
```

**Remarks**

`RequiredObjects` returns a table listing the objects required by the specified object `AObjectID` or `AObjectName`. If `ARecursive` is `true` (or omitted) ALL objects required by the specified object will be listed. This includes any objects required by the objects that are required by `AObjectID` or `AObjectName`, and so on. If `ARecursive` is `false` only the objects required by the specified object itself will be listed.

**Using System.RequiredObjects**

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
create view View Table where Name = "Alphora";
select RequiredObjects("View", false) over { Object_Name, Sequence, Level };
select RequiredObjects("View", true) over { Object_Name, Sequence, Level };
UnregisterLibrary("Lib");
DropLibrary("Lib");
```

```d4
Object_Name                                Sequence Level
------------------------------------------ -------- -----
System.iEqual_System_String_System_String_ 1        1
Lib.Table                                  2        1
System.iRetrieve__                         3        1
System.iRestrict_table_System_Boolean_     4        1
```

```d4
Object_Name                                Sequence Level
------------------------------------------ -------- -----
System.iEqual_System_String_System_String_ 1        1
System.String                              2        2
System.Boolean                             3        2
Lib.Table                                  4        1
System.String                              5        2
System.Integer                             6        2
Lib.Device                                 7        2
System.iRetrieve__                         8        1
System.iRestrict_table_System_Boolean_     9        1
System.Boolean                             10       2
```

**See Also**

[Objects](../../../developers-guide/d4-catalog.md#objects) \| [System.DependentObjects](System.DependentObjects.md) \| [System.ObjectExists](System.ObjectExists.md) \| [System.ObjectID](System.ObjectID.md) \| [System.ObjectName](System.ObjectName.md)
