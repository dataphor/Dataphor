# System.JoinApplicationTransaction

**Declarations**

```d4
create operator .System.JoinApplicationTransaction(const AID : System.Guid, const AIsInsert : System.Boolean)
 class "System.JoinApplicationTransactionNode" static tags { DAE.IsRemotable = "False", DAE.IsFunctional = "False" };
```

**Remarks**

`JoinApplicationTransaction` allows the process to join the specified application transaction in progress. For more information about application transactions, see the corresponding entry in the Dataphor Developers guide.

**See Also**

[Application Transactions, Presentation Layer](../../../developers-guide/space-between-data.md#application-transactions) \| [Application Transactions, Application Development](../../../developers-guide/dataphor-product-anatomy.md#application-transactions) \| [DefaultUseImplicitTransactions](../../../users-guide/process-defaults.md#defaultuseimplicittransactions) \| [System.BeginApplicationTransaction](System.BeginApplicationTransaction.md) \| [System.LeaveApplicationTransaction](System.LeaveApplicationTransaction.md)
