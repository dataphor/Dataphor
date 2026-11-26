# System.SetUseImplicitTransactions

**Declarations**

```d4
create operator .System.SetUseImplicitTransactions(const AUseImplicitTransactions : System.Boolean)
 class "System.SystemSetUseImplicitTransactionsNode" static tags { DAE.IsRemotable = "False", DAE.IsFunctional = "False" };
```

**Remarks**

`SetUseImplicitTransactions` takes a boolean argument. It specifies if the current session will use implicit transactions.

**Using System.SetUseImplicitTransactions**

```d4
SetUseImplicitTransactions(true);
```

**See Also**

[System.SetDefaultUseImplicitTransactions](System.SetDefaultUseImplicitTransactions.md) \| [Transactional Call Protocol](../../../users-guide/process-defaults.md#defaultuseimplicittransactions)
