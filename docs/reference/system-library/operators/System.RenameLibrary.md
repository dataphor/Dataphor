# System.RenameLibrary

**Declarations**

```d4
create operator .System.RenameLibrary(const AOldName : System.Name, const ANewName : System.Name)
 class "System.SystemRenameLibraryNode" static tags { DAE.IsRemotable = "False", DAE.IsFunctional = "False" };
```

**Remarks**

`RenameLibrary` changes the name of the library from `AOldName` to `ANewName`. The library must be unloaded.

**Using System.RenameLibrary**

```d4
CreateLibrary(LibraryDescriptor("Lib", VersionNumberUndefined(), "Temp"));
RegisterLibrary("Lib");
select LibraryName();
UnregisterLibrary("Lib");
RenameLibrary("Lib", "OtherLib");
DropLibrary("OtherLib");
```

```text
Lib
```

**See Also**

[System.CreateLibrary](System.CreateLibrary.md) \| [System.RegisterLibrary](System.RegisterLibrary.md) \| [System.SetLibraryDescriptor](System.SetLibraryDescriptor.md) \| [System.UnregisterLibrary](System.UnregisterLibrary.md) \| [System.DropLibrary](System.DropLibrary.md)
