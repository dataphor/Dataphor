# System.RemoveAt

**Declarations**

```d4
create operator .System.RemoveAt(var AList : generic list, const AInteger : System.Integer)
 class "System.ListRemoveAtNode" static tags { DAE.IsFunctional = "False" };
```

**Remarks**

`RemoveAt` removes the item at the specified index from the specified list. If the index is not in the list then an exception is thrown.

> [!NOTE]
> String and list indexes are zero based.

**Using System.RemoveAt**

```d4
begin
 var CurrList : list :=
 {
  "Michelle",
  "Melinda",
  "Melissa",
  "Melody",
  "Margaret"
 };
 CurrList.RemoveAt(1);
end;
```

**See Also**

[System.Remove](System.Remove.md) \| [System.Insert](System.Insert.md) \| [Using Lists](../../../developers-guide/modeling-process-logic.md#using-lists)
