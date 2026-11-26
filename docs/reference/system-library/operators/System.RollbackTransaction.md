# System.RollbackTransaction

**Declarations**

```d4
create operator .System.RollbackTransaction()
 class "System.SystemRollbackTransactionNode" static tags { DAE.IsRemotable = "False", DAE.IsFunctional = "False" };
```

**Remarks**

`RollbackTransaction` rolls back the current transaction. All changes that were made under this transaction are undone and the transaction is ended.

**Using System.RollbackTransaction**

```d4
begin
    BeginTransaction();
    var TempCursor : cursor(table { ID: Integer, Name : String});
    TempCursor := cursor(Employee capabilities { Updateable });
    TempCursor.First();
    TempCursor.Next();
    TempCursor.Insert( row { 10 ID, "Juan" Name});
    PrepareTransaction();
    RollbackTransaction();
end;
```

**See Also**

[Transaction Management, Application Design](../../../developers-guide/modeling-process-logic.md#transaction-management) \| [Transaction Management, Database Management Concepts](../../../developers-guide/database-management-concepts.md#transaction-management) \| [DefaultUseImplicitTransactions](../../../users-guide/process-defaults.md#defaultuseimplicittransactions) \| [System.BeginTransaction](System.BeginTransaction.md)
