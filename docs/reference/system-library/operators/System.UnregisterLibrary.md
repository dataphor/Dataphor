# System.UnregisterLibrary

**Declarations**

```d4
create operator .System.UnregisterLibrary(const AName : System.Name)
 class "System.SystemUnregisterLibraryNode" static tags { DAE.IsRemotable = "False", DAE.IsFunctional = "False" };
create operator .System.UnregisterLibrary(const AName : System.Name, const AWithReconciliation : System.Boolean)
 class "System.SystemUnregisterLibraryNode" static tags { DAE.IsRemotable = "False", DAE.IsFunctional = "False" };
```

**Remarks**

`UnregisterLibrary` drops all objects in the library and issues corresponding drop statements to devices.

The `AWithReconciliation` option can be used to indicate that the unregistration process should not affect the structure or data mapped in devices. If the unregistration is performed with reconciliation, data loss will occur because the corresponding drop commands will be issued to the target systems.

**Using System.UnregisterLibrary**

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

[Reconciliation and Registering](../../../users-guide/dataphoria-libraries.md#reconciliation-and-registering) \| [System.CreateLibrary](System.CreateLibrary.md) \| [System.RegisterLibrary](System.RegisterLibrary.md) \| [System.SetLibraryDescriptor](System.SetLibraryDescriptor.md) \| [System.DropLibrary](System.DropLibrary.md)
