# System.SaveLibraryCatalog

**Declarations**

```d4
create operator .System.SaveLibraryCatalog(const ALibraryName : System.Name)
 class "System.SystemSaveLibraryCatalogNode" static tags { DAE.IsRemotable = "False", DAE.IsFunctional = "False" };
```

**Remarks**

`SaveLibraryCatalog` is used during development as a checkpoint save of the specified library.

**Using System.SaveLibraryCatalog**

```d4
SaveLibraryCatalog("Lib");
```

**See Also**

[Server](../../../users-guide/dataphor-explorer.md#server)
