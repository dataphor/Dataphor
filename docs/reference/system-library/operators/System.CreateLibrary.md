# System.CreateLibrary

**Declarations**

```d4
create operator .System.CreateLibrary(const ALibraryDescriptor : System.LibraryDescriptor)
 class "System.SystemCreateLibraryNode" static tags { DAE.IsRemotable = "False", DAE.IsFunctional = "False" };
```

**Remarks**

`CreateLibrary` creates a new library using the specified LibraryDescriptor.

**Using System.CreateLibrary**

```d4
CreateLibrary(LibraryDescriptor("Lib", VersionNumberUndefined(), "Temp"));
RegisterLibrary("Lib");
select LibraryName();
UnregisterLibrary("Lib");
DropLibrary("Lib");
```

```text
Lib
```

**See Also**

[System.RegisterLibrary](System.RegisterLibrary.md) \| [System.ReguiredLibraries](System.RequiredLibraries.md) \| [System.UnregisterLibrary](System.UnregisterLibrary.md) \| [System.SetLibrary](System.SetLibrary.md) \| [System.SetLibraryDescriptor](System.SetLibraryDescriptor.md)
