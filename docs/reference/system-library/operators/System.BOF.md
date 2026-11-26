# System.BOF

**Declarations**

```d4
create operator .System.BOF(const ACursor : generic cursor) : System.Boolean
 class "System.CursorBOFNode";
```

**Remarks**

`BOF` checks to see if the cursor specified is at the beginning of the current recordset. Returns true if the cursor is at the beginning, false if it is anywhere else.

**Using System.BOF**

```d4
begin
    var Output : String := "";
    var TempCursor : cursor(table { ID: Integer, Name : String});
    TempCursor := cursor(Employee capabilities { Updateable });
    TempCursor.Last();
    TempCursor.Prior();
    while not TempCursor.BOF() do
    begin
        Output := Output + String.Unicode({13})+ Name from TempCursor.Select();
        TempCursor.Prior();
    end;
    raise Error(Output + Unicode({13}));
end;
```

**See Also**

[System.EOF](System.EOF.md) \| [Using Cursors](../../../developers-guide/modeling-process-logic.md#using-cursors)
