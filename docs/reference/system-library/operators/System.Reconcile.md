# System.Reconcile

**Declarations**

```d4
create operator .System.Reconcile() : table { Sequence : System.Integer, Error : System.Error }
 class "System.SystemReconcileNode" static tags { DAE.IsRemotable = "False", DAE.IsFunctional = "False" };
create operator .System.Reconcile(const ADeviceName : System.Name) : table { Sequence : System.Integer, Error : System.Error }
 class "System.SystemReconcileNode" static tags { DAE.IsRemotable = "False", DAE.IsFunctional = "False" };
create operator .System.Reconcile(const ADeviceName : System.Name, const ATableName : System.Name) : table { Sequence : System.Integer, Error : System.Error }
 class "System.SystemReconcileNode" static tags { DAE.IsRemotable = "False", DAE.IsFunctional = "False" };
```

**Remarks**

`Reconcile` reconciles with the specified device. Reconciliation adds items in the device to the Dataphor system catalog.

**Using System.Reconcile**

MSSQLDevice Documentation Example

**See Also**

Reconciliation \| [ShouldReconcile](../../tags.md#shouldreconcile)
