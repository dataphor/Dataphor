# System.SetDefaultUseImplicitTransactions

**Declarations**

```d4
create operator .System.SetDefaultUseImplicitTransactions(const ADefaultUseImplicitTransactions : System.Boolean)
 class "System.SystemSetDefaultUseImplicitTransactionsNode" static tags { DAE.IsRemotable = "False", DAE.IsFunctional = "False" };
```

**Remarks**

`SetDefaultUseImplicitTransactions` takes a boolean argument. It specifies if processes created from this session will use implicit transactions.

**Using System.SetDefaultUseImplicitTransactions**

```d4
SetDefaultUseImplicitTransactions(true);
```

**See Also**

[System.UseImplicitTransactions](System.SetUseImplicitTransactions.md) \| [Transactional Call Protocol](../../../users-guide/process-defaults.md#defaultuseimplicittransactions) \| [DefaultUseImplicitTransactions](../../../users-guide/process-defaults.md#defaultuseimplicittransactions) \| [Session Information](../../../users-guide/aliases.md#session-information)
