# System.RefreshLibraries

**Declarations**

```d4
create operator .System.RefreshLibraries()
 class "System.SystemRefreshLibrariesNode" static tags { DAE.IsRemotable = "False", DAE.IsFunctional = "False" };
```

**Remarks**

`RefreshLibraries` ensures that the Dataphor server has all the libraries in the library directory. There are no arguments and nothing is returned.

**Using System.RefreshLibraries**

```d4
RefreshLibraries();
```

**See Also**

[Library Implementation](../../../developers-guide/working-with-libraries.md#library-implementation)
