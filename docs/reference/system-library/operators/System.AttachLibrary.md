# System.AttachLibrary

**Declarations**

```d4
create operator .System.AttachLibrary(const AName : System.Name, const ADirectory : System.String)
 class "System.SystemAttachLibraryNode" static tags { DAE.IsRemotable = "False", DAE.IsFunctional = "False" };
```

**Remarks**

`AttachLibrary` attaches an existing library to the Dataphor Server and makes that library avaiable for registration or loading.

**See Also**

[System.DetachLibrary](System.DetachLibrary.md) \| [System.CreateLibrary](System.CreateLibrary.md) \| [Working With Libraries](../../../developers-guide/working-with-libraries.md)
