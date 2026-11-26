# System.Diagnostics.DisableErrorLogging

**Declarations**

```d4
create operator .System.Diagnostics.DisableErrorLogging()
 class "System.SystemDisableErrorLoggingNode" static tags { DAE.IsRemotable = "False", DAE.IsFunctional = "False" };
```

**Remarks**

`DisableErrorLogging` stops the logging of errors that cross the CLI boundry.

**Using System.Diagnostics.DisableErrorLogging**

```d4
DisableErrorLogging();
```

**See Also**

[Configuring the Dataphor Service](../../../users-guide/dataphor-service.md#configuration) \| [System.Diagnostics.EnableErrorLogging](System.Diagnostics.EnableErrorLogging.md)
