# System.LibraryName

**Declarations**

```d4
create operator .System.LibraryName() : System.Name
 class "System.SystemLibraryNameNode" static tags { DAE.IsRemotable = "False" };
create operator .System.LibraryName(const AObjectName : System.Name) : System.Name
 class "System.SystemLibraryNameNode" static tags { DAE.IsRemotable = "False" };
create operator .System.LibraryName(const AObjectID : System.Integer) : System.Name
 class "System.SystemLibraryNameNode" static tags { DAE.IsRemotable = "False" };
```

**Remarks**

`LibraryName` returns the current library name if no argument is provided.

`LibraryName` returns the name of the library with the specified ObjectID if the argument, `AObjectID`, is provided.

`LibraryName` returns the name of the library with the specified ObjectName if the argument, `AObjectName`, is provided.

**Using System.LibraryName**

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
