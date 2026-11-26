# System.LeaveApplicationTransaction

**Declarations**

```d4
create operator .System.LeaveApplicationTransaction()
 class "System.LeaveApplicationTransactionNode" static tags { DAE.IsRemotable = "False", DAE.IsFunctional = "False" };
```

**Remarks**

`LeaveApplicationTransaction` allows the process to exit the specified application transaction without ending the transaction. For more information about application transactions, see the corresponding entry in the Dataphor Developers guide.

**See Also**

[Application Transactions, Presentation Layer](../../../developers-guide/space-between-data.md#application-transactions) \| [Application Transactions, Application Development](../../../developers-guide/dataphor-product-anatomy.md#application-transactions) \| [System.BeginApplicationTransaction](System.BeginApplicationTransaction.md) \| [System.JoinApplicationTransaction](System.JoinApplicationTransaction.md)
