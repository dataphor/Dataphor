# System.Clear

**Declarations**

```d4
create operator .System.Clear(var AList : generic list)
 class "System.ListClearNode" static tags { DAE.IsFunctional = "False" };
```

**Remarks**

`Clear` clears the specified list.

**Using System.Clear**

```d4
begin
 var EmpList : list(String) := list(String){};
 var EmpCur : cursor(table{ID: Integer, Name: String});
 EmpCur := cursor(Employee);
 EmpCur.Next();
 while not EmpCur.EOF() do
 begin
  EmpList.Add(Name from EmpCur.Select());
  EmpCur.Next();        
 end;
 EmpList.Clear();
end;
```

**See Also**

[System.Remove](System.Remove.md) \| [Using Lists](../../../developers-guide/modeling-process-logic.md#using-lists)
