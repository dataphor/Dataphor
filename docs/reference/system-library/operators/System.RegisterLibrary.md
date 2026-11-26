# System.RegisterLibrary

**Declarations**

```d4
create operator .System.RegisterLibrary(const AName : System.Name)
 class "System.SystemRegisterLibraryNode" static tags { DAE.IsRemotable = "False", DAE.IsFunctional = "False" };
create operator .System.RegisterLibrary(const AName : System.Name, const AWithReconciliation : System.Boolean)
 class "System.SystemRegisterLibraryNode" static tags { DAE.IsRemotable = "False", DAE.IsFunctional = "False" };
```

**Remarks**

`RegisterLibrary` registers the specified library so that it can be used. For more information on libraries, see the Working With Libraries chapter in the Dataphor Developer's Guide.

The `AWithReconciliation` option indicates whether device-level communication should be enabled while registering the library. For more information, see the [Reconciliation and Registering](../../../users-guide/dataphoria-libraries.md#reconciliation-and-registering) discussion in the Dataphor User's Guide.

When a library is registered, the following sequence of steps is taken in the order listed:

- The Dataphor Server ensures that the required version of each requisite library is registered.

<!-- -->

- Each file listed in the library is copied into the run-time directory of the Dataphor Server. Copy errors are ignored so that assemblies that are already registered will not prevent the library from being registered. (Note that this means the Dataphor Server must be restarted to update assemblies which have been changed.)

<!-- -->

- Each file that is marked as an assembly is registered with the Dataphor Server.

<!-- -->

- The library is set as the current library for the session.

<!-- -->

- If there is a file called Documents\Register.d4 in the library directory, it is run as the registration script for the library.

**Using System.RegisterLibrary**

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

[System.UnregisterLibrary](System.UnregisterLibrary.md) \| [System.SetLibrary](System.SetLibrary.md) \| [Reconciliation and Registering](../../../users-guide/dataphoria-libraries.md#reconciliation-and-registering)
