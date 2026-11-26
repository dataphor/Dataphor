# System.SetLibraryDescriptor

**Declarations**

```d4
create operator .System.SetLibraryDescriptor(const AName : System.Name, const ADescriptor : System.LibraryDescriptor)
 class "System.SystemSetLibraryDescriptorNode" static tags { DAE.IsRemotable = "False", DAE.IsFunctional = "False" };
```

**Remarks**

`SetLibraryDescriptor` is used to modify the settings of the library `AName` using the LibraryDescriptor `ADescriptor`.

**Using System.SetLibraryDescriptor**

```d4
CreateLibrary
(
 LibraryDescriptor
 (
  "Lib",
  VersionNumberUndefined(),
  "Temp"
 )
);
select Libraries
 where Name = "Lib"
 over { Name, Version, DefaultDeviceName };
SetLibraryDescriptor
( "Lib",
 LibraryDescriptor
 (
  "Lib",
  ToVersionNumber("1.0.*"),
  "Temp"
 )
);
select Libraries
 where Name = "Lib"
 over { Name, Version, DefaultDeviceName };
DropLibrary("Lib");
```

```text
Name  Version DefaultDeviceName
----- ------- -----------------
MyLib *       Temp

Name  Version DefaultDeviceName
----- ------- -----------------
MyLib 1.0.*   Temp
```

**See Also**

[System.CreateLibrary](System.CreateLibrary.md) \| [System.RegisterLibrary](System.RegisterLibrary.md) \| [System.ReguiredLibraries](System.RequiredLibraries.md) \| [System.UnregisterLibrary](System.UnregisterLibrary.md) \| [System.SetLibrary](System.SetLibrary.md)
