# System.TransactionCount

**Declarations**

```d4
create operator .System.TransactionCount() : System.Integer
 class "System.SystemTransactionCountNode" static tags { DAE.IsRemotable = "False", DAE.IsLiteral = "False" };
```

**Remarks**

`TransactionCount` returns the current level of nested transactions. The transaction count returned includes implicit transactions, but does not include application transactions.

**Using System.TransactionCount**

```d4
select TableDee add {TransactionCount() Transactions};
```

```text
Transactions
------------
1
```

**See Also**

[Transaction Management, Application Design](../../../developers-guide/modeling-process-logic.md#transaction-management) \| [Transaction Management, Database Management Concepts](../../../developers-guide/database-management-concepts.md#transaction-management) \| [DefaultUseImplicitTransactions](../../../users-guide/process-defaults.md#defaultuseimplicittransactions) \| [System.BeginTransaction](System.BeginTransaction.md)
