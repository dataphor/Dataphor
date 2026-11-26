# System.RequiredLibraries

**Declarations**

```d4
create operator .System.RequiredLibraries(const ALibraryName : System.Name) : table { Library_Name : System.Name, Sequence : System.Integer, Level : System.Integer }
 class "System.SystemRequiredLibrariesNode" static tags { DAE.IsRemotable = "False" };
create operator .System.RequiredLibraries(const ALibraryName : System.Name, const ARecursive : System.Boolean) : table { Library_Name : System.Name, Sequence : System.Integer, Level : System.Integer }
 class "System.SystemRequiredLibrariesNode" static tags { DAE.IsRemotable = "False" };
```

**Remarks**

`RequiredLibraries` returns a table listing the libraries required by the specified library `ALibraryName`. If `ARecursive` is `true` (or omitted) ALL libraries required to register `ALibraryName` will be listed. This includes any libraries required by the libraries that are required by `ALibraryName`, and so on. If `ARecursive` is `false` only the libraries required by `ALibraryName` itself will be listed.

**Using System.RequiredLibraries**

```d4
CreateLibrary
 (LibraryDescriptor
  ("Lib", VersionNumberUndefined(), "Device",
   list(FileReference) {},
   list(LibraryReference)
    { LibraryReference("MSSQLDevice",VersionNumberUndefined()) }
 ));
RegisterLibrary("Lib");
select RequiredLibraries("Lib", false);
select RequiredLibraries("Lib", true);
UnregisterLibrary("Lib");
DropLibrary("Lib");
```

```text
Library_Name Sequence Level 
------------ -------- ----- 
MSSQLDevice  1        1
```

```text
Library_Name Sequence Level 
------------ -------- ----- 
MSSQLDevice  1        1     
System       2        2     
SQLDevice    3        2     
System       4        3     
Connection   5        3     
System       6        4
```

**See Also**

[System.DependentLibraries](System.DependentLibraries.md) \| [System.CreateLibrary](System.CreateLibrary.md) \| [System.RegisterLibrary](System.RegisterLibrary.md) \| [System.UnregisterLibrary](System.UnregisterLibrary.md) \| [System.SetLibrary](System.SetLibrary.md) \| [System.SetLibraryDescriptor](System.SetLibraryDescriptor.md)
