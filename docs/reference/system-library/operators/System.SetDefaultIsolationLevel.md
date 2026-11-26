# System.SetDefaultIsolationLevel

**Declarations**

```d4
create operator .System.SetDefaultIsolationLevel(const AIsolationLevel : System.String)
 class "System.SystemSetDefaultIsolationLevelNode" static tags { DAE.IsRemotable = "False", DAE.IsFunctional = "False" };
```

**Remarks**

`SetDefaultIsolationLevel` sets the isolation level that will be used by the session as the default isolation level for any new processes started on the session. This in turn is used as the isolation level for any transactions started on the process unless another isolation level is specified, or the process default isolation level is changed using `SetIsolationLevel`.

`AIsolationLevel` must be one of `Browse`, `Cursor Stability`, or `Isolated`

**Using System.SetDefaultIsolationLevel**

```d4
SetDefaultIsolationLevel("Cursor Stability");
```

**See Also**

[DBM Concepts Transaction Management](../../../developers-guide/database-management-concepts.md#transaction-management) \| [Isolation](../../../developers-guide/database-management-concepts.md#isolation) \| [DefaultIsolationLevel](../../../users-guide/process-defaults.md#defaultisolationlevel) \| [Session Information](../../../users-guide/aliases.md#session-information)
