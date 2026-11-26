# System.DetachLibrary

**Declarations**

```d4
create operator .System.DetachLibrary(const AName : System.Name)
 class "System.SystemDetachLibraryNode" static tags { DAE.IsRemotable = "False", DAE.IsFunctional = "False" };
```

**Remarks**

`DetachLibrary` detaches an available library from the Dataphor Server. The library must be unloaded in order to be attached. Once a library is detached, it will no longer be available for reference by other libraries. Note that if the library is contained within one of the configured library directories for the Dataphor Server, it will be available again the next time the server is started.

**See Also**

[System.AttachLibrary](System.AttachLibrary.md) \| [System.DropLibrary](System.DropLibrary.md) \| [Working With Libraries](../../../developers-guide/working-with-libraries.md)
