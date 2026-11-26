# System.Close

**Declarations**

```d4
create operator .System.Close(const ACursor : generic cursor)
 class "System.CursorCloseNode";
```

**Remarks**

`Close` closes the specified cursor.

**Using System.Close**

```d4
begin
    var EmpCursor : cursor(table{ID : Integer, Name : String});
    var Employees : String := "";
    EmpCursor := cursor(Employee);
    try
        while EmpCursor.Next() do
        begin
            Employees := Employees + Unicode({13}) + (Name from EmpCursor.Select());
        end;
    finally
        EmpCursor.Close();
        raise Error(Employees);
    end;
end;
```

```text
Joe
John
Josh
Jeff
Jake
Jeff
```

**See Also**

[Using Cursors](../../../developers-guide/modeling-process-logic.md#using-cursors) \| [Cursors Values](../../../developers-guide/d4-language-elements.md#cursor-values)
