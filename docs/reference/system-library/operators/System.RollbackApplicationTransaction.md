# System.RollbackApplicationTransaction

**Declarations**

```d4
create operator .System.RollbackApplicationTransaction(const AID : System.Guid)
 class "System.RollbackApplicationTransactionNode" static tags { DAE.IsRemotable = "False", DAE.IsFunctional = "False" };
```

**Remarks**

`RollbackApplicationTransaction` rolls back the specified application transaction. All changes that were made under this application transaction are undone and the application transaction is ended. For more information about application transactions, see the corresponding entry in the Dataphor Developers guide.

**See Also**

[Application Transactions, Presentation Layer](../../../developers-guide/space-between-data.md#application-transactions) \| [Application Transactions, Application Development](../../../developers-guide/dataphor-product-anatomy.md#application-transactions) \| [DefaultUseImplicitTransactions](../../../users-guide/process-defaults.md#defaultuseimplicittransactions) \| [System.BeginApplicationTransaction](System.BeginApplicationTransaction.md) \| [System.JoinApplicationTransaction](System.JoinApplicationTransaction.md) \| [System.PrepareApplicationTransaction](System.PrepareApplicationTransaction.md) \| [System.CommitApplicationTransaction](System.CommitApplicationTransaction.md)
