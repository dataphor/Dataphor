# System.GetLibraryDescriptor

**Declarations**

```d4
create operator .System.GetLibraryDescriptor(const AName : System.Name) : System.LibraryDescriptor
 class "System.SystemGetLibraryDescriptorNode" static tags { DAE.IsRemotable = "False" };
```

**Remarks**

`GetLibraryDescriptor` returns the LibraryDescriptor for the specified library.

**Using System.GetLibraryDescriptor**

```d4
CreateLibrary(LibraryDescriptor("Lib", VersionNumberUndefined(), "Temp"));
select GetLibraryDescriptor("Lib").Name;
select GetLibraryDescriptor("Lib").Version;
select GetLibraryDescriptor("Lib").DefaultDeviceName;
select GetLibraryDescriptor("Lib").Files.Count();
select GetLibraryDescriptor("Lib").Requisites.Count();
DropLibrary("Lib");
```

```text
Lib
*
Temp
0
0
```

**See Also**

[System.RegisterLibrary](System.RegisterLibrary.md) \| [System.UnregisterLibrary](System.UnregisterLibrary.md) \| [System.SetLibrary](System.SetLibrary.md)
