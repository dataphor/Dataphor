# System.DependentLibraries

**Declarations**

```d4
create operator .System.DependentLibraries(const ALibraryName : System.Name) : table { Library_Name : System.Name, Sequence : System.Integer, Level : System.Integer }
 class "System.SystemDependentLibrariesNode" static tags { DAE.IsRemotable = "False" };
create operator .System.DependentLibraries(const ALibraryName : System.Name, const ARecursive : System.Boolean) : table { Library_Name : System.Name, Sequence : System.Integer, Level : System.Integer }
 class "System.SystemDependentLibrariesNode" static tags { DAE.IsRemotable = "False" };
```

**Remarks**

`DependentLibraries` returns a table listing the libraries that depend on the specified library `ALibraryName`. If `ARecursive` is `true` (or omitted) ALL libraries that depend on `ALibraryName` will be listed. This includes any libraries that depend on the libraries that depend on `ALibraryName`, and so on. If `ARecursive` is `false` only the libraries that depend on `ALibraryName` itself will be listed.

**Using System.DependentLibraries**

```d4
select DependentLibraries("System", false) where Sequence < 5;
select DependentLibraries("System", true) where Sequence < 5;
```

```text
Library_Name Sequence Level
------------ -------- -----
General      1        1
SimpleDevice 2        1
Connection   3        1
SQLDevice    4        1
```

```text
Library_Name Sequence Level
------------ -------- -----
General      1        1
SimpleDevice 2        1
Frontend     3        2
Security     4        3
```

**See Also**

[System.RequiredLibraries](System.RequiredLibraries.md) \| [System.CreateLibrary](System.CreateLibrary.md) \| [System.RegisterLibrary](System.RegisterLibrary.md) \| [System.UnregisterLibrary](System.UnregisterLibrary.md) \| [System.SetLibrary](System.SetLibrary.md) \| [System.SetLibraryDescriptor](System.SetLibraryDescriptor.md)
