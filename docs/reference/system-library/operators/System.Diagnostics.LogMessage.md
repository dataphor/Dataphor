# System.Diagnostics.LogMessage

**Declarations**

```d4
create operator .System.Diagnostics.LogMessage(const AMessage : System.String)
 class "System.SystemLogMessageNode" static tags { DAE.IsRemotable = "False", DAE.IsFunctional = "False" };
```

**Remarks**

`LogMessage` writes the specified message `AMessage` to the Dataphor log.

**Using System.Diagnostics.LogMessage**

```d4
LogMessage("This is a test Message");
```

```text
Entries in log file read:

2004-05-24 10:24:29.2745 Event: LogStarted
2004-05-24 10:24:41.7590 Event: ServerStarted
2004-05-24 10:59:13.2530 This is a test Message
```

**See Also**

[System.Diagnostics.DisableErrorLogging](System.Diagnostics.DisableErrorLogging.md) \| [System.Diagnostics.EnableErrorLogging](System.Diagnostics.EnableErrorLogging.md) \| [System.Diagnostics.LogError](System.Diagnostics.LogError.md)
