# System.CommitTransaction

**Declarations**

```d4
create operator .System.CommitTransaction()
 class "System.SystemCommitTransactionNode" static tags { DAE.IsRemotable = "False", DAE.IsFunctional = "False" };
```

**Remarks**

`CommitTransaction` commits all that has occurred since the beginning of the current transaction.

**Using System.CommitTransaction**

```d4
begin
    BeginTransaction();
    var TempCursor : cursor(table { ID: Integer, Name : String});
    TempCursor := cursor(Employee capabilities { Updateable });
    TempCursor.First();
    TempCursor.Next();
    TempCursor.Insert( row { 12 ID, "Joel" Name});
    TempCursor.Delete();
    PrepareTransaction();
    RollbackTransaction();
end;
```

**See Also**

[Transaction Management, Application Design](../../../developers-guide/modeling-process-logic.md#transaction-management) \| [Transaction Management, Database Management Concepts](../../../developers-guide/database-management-concepts.md#transaction-management) \| [DefaultUseImplicitTransactions](../../../users-guide/process-defaults.md#defaultuseimplicittransactions) \| [System.BeginTransaction](System.BeginTransaction.md) \| [System.PrepareTransaction](System.PrepareTransaction.md) \| [System.RollbackTransaction](System.RollbackTransaction.md) \| [System.TransactionCount](System.TransactionCount.md) \| [System.InTransaction](System.InTransaction.md)
