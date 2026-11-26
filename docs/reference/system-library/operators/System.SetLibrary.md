# System.SetLibrary

**Declarations**

```d4
create operator .System.SetLibrary(const AName : System.Name)
 class "System.SystemSetLibraryNode" static tags { DAE.IsRemotable = "False", DAE.IsFunctional = "False" };
```

**Remarks**

`SetLibrary` sets the specified library as the current library for the session.

**Using System.SetLibrary**

```d4
CreateLibrary(LibraryDescriptor("Lib", VersionNumberUndefined(), "Temp"));
RegisterLibrary("Lib");
select LibraryName();
SetLibrary("Lib");
select LibraryName();
UnregisterLibrary("Lib");
DropLibrary("Lib");
```

```text
Lib
General
Lib
```

**See Also**

[System.CreateLibrary](System.CreateLibrary.md) \| [System.RegisterLibrary](System.RegisterLibrary.md) \| [System.UnregisterLibrary](System.UnregisterLibrary.md) \| [System.SetLibraryDescriptor](System.SetLibraryDescriptor.md) \| [System.DropLibrary](System.DropLibrary.md)
