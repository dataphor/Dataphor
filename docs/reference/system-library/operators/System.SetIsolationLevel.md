# System.SetIsolationLevel

**Declarations**

```d4
create operator .System.SetIsolationLevel(const AIsolationLevel : System.String)
 class "System.SystemSetIsolationLevelNode" static tags { DAE.IsRemotable = "False", DAE.IsFunctional = "False" };
```

**Remarks**

`SetIsolationLevel` sets the isolation level that will be used by the process for any new transactions unless another isolation level is specified.

`AIsolationLevel` must be one of `Browse`, `Cursor Stability`, or `Isolated`

**Using System.SetIsolationLevel**

```d4
SetIsolationLevel("Cursor Stability");
```

**See Also**

[DBM Concepts Transaction Management](../../../developers-guide/database-management-concepts.md#transaction-management) \| [Isolation](../../../developers-guide/database-management-concepts.md#isolation) \| [DefaultIsolationLevel](../../../users-guide/process-defaults.md#defaultisolationlevel) \| [Session Information](../../../users-guide/aliases.md#session-information)
