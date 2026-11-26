# System.PrepareApplicationTransaction

**Declarations**

```d4
create operator .System.PrepareApplicationTransaction(const AID : System.Guid)
 class "System.PrepareApplicationTransactionNode" static tags { DAE.IsRemotable = "False", DAE.IsFunctional = "False" };
```

**Remarks**

`PrepareApplicationTransaction` prepares the application transaction for for the final commit. Executing the `PrepareApplicationTransaction` operator checks all of the integrity constraints and ensures that everything is ready in this application transaction to commit.

**See Also**

[Application Transactions, Presentation Layer](../../../developers-guide/space-between-data.md#application-transactions) \| [Application Transactions, Application Development](../../../developers-guide/dataphor-product-anatomy.md#application-transactions) \| [DefaultUseImplicitTransactions](../../../users-guide/process-defaults.md#defaultuseimplicittransactions) \| [System.BeginApplicationTransaction](System.BeginApplicationTransaction.md) \| [System.JoinApplicationTransaction](System.JoinApplicationTransaction.md) \| [System.CommitApplicationTransaction](System.CommitApplicationTransaction.md) \| [System.RollbackApplicationTransaction](System.RollbackApplicationTransaction.md)
