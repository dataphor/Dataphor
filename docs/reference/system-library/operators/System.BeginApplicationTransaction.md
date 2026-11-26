# System.BeginApplicationTransaction

**Declarations**

```d4
create operator .System.BeginApplicationTransaction() : System.Guid
 class "System.BeginApplicationTransactionNode" static tags { DAE.IsRemotable = "False", DAE.IsFunctional = "False" };
create operator .System.BeginApplicationTransaction(const AShouldJoin : System.Boolean, const AIsInsert : System.Boolean) : System.Guid
 class "System.BeginApplicationTransactionNode" static tags { DAE.IsRemotable = "False", DAE.IsFunctional = "False" };
```

**Remarks**

`BeginApplicationTransaction` begins an application transaction. An application transaction is an optimistic transaction. It does not take locks on the requested data, but performs an optimistic concurrency check prior to updating the changed data from the created cache. For more information see The Space Between the Data in Part III of the Dataphor Developers Guide.

**See Also**

[Application Transactions, Presentation Layer](../../../developers-guide/space-between-data.md#application-transactions) \| [Application Transactions, Application Development](../../../developers-guide/dataphor-product-anatomy.md#application-transactions) \| [System.JoinApplicationTransaction](System.JoinApplicationTransaction.md) \| [System.PrepareApplicationTransaction](System.PrepareApplicationTransaction.md) \| [System.CommitApplicationTransaction](System.CommitApplicationTransaction.md) \| [System.RollbackApplicationTransaction](System.RollbackApplicationTransaction.md)
