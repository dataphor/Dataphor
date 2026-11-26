# System.CommitApplicationTransaction

**Declarations**

```d4
create operator .System.CommitApplicationTransaction(const AID : System.Guid)
 class "System.CommitApplicationTransactionNode" static tags { DAE.IsRemotable = "False", DAE.IsFunctional = "False" };
```

**Remarks**

CommitApplicationTransaction commits all that has occurred since the beginning of the current application transaction. For more information about application transactions, see the corresponding entry in the Dataphor Developers guide.

**See Also**

[Application Transactions, Presentation Layer](../../../developers-guide/space-between-data.md#application-transactions) \| [Application Transactions, Application Development](../../../developers-guide/dataphor-product-anatomy.md#application-transactions) \| [System.BeginApplicationTransaction](System.BeginApplicationTransaction.md) \| [System.JoinApplicationTransaction](System.JoinApplicationTransaction.md) \| [System.PrepareApplicationTransaction](System.PrepareApplicationTransaction.md) \| [System.RollbackApplicationTransaction](System.RollbackApplicationTransaction.md)
