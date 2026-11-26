# System.Add

**Declarations**

```d4
create operator .System.Add(var AList : generic list, AValue : generic) : System.Integer
 class "System.ListAddNode" static tags { DAE.IsFunctional = "False" };
```

**Remarks**

Adds an element to a list. It returns the index of the new item. This index will be one less than the size of the list after the insert.

**Using System.Add**

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
end;
```

**See Also**

[Using Lists](../../../developers-guide/modeling-process-logic.md#using-lists) \| [List Types](../../../developers-guide/d4-language-elements.md#list-types) \| [List Values](../../../developers-guide/d4-language-elements.md#list-values)
