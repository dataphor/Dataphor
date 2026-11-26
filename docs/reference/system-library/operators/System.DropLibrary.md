# System.DropLibrary

**Declarations**

```d4
create operator .System.DropLibrary(const AName : System.Name)
 class "System.SystemDropLibraryNode" static tags { DAE.IsRemotable = "False", DAE.IsFunctional = "False" };
```

**Remarks**

`DropLibrary` deletes the specified library. To drop a library it must be unloaded or unregistered.

**Using System.DropLibrary**

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

[System.RegisterLibrary](System.RegisterLibrary.md) \| [System.UnregisterLibrary](System.UnregisterLibrary.md) \| [System.SetLibrary](System.SetLibrary.md)
