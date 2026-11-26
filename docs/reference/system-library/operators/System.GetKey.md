# System.GetKey

**Declarations**

```d4
create operator .System.GetKey(const ACursor : generic cursor) : generic row
 class "System.CursorGetKeyNode";
```

**Remarks**

`GetKey` returns a row with structure the same as the searchable key, with data that is the same as the data in those columns in the current row. This information can be saved in a row variable and used later in `FindKey`. This is similar to the bookmark, except that the data and structure of a bookmark is unimportant.

**Using System.GetKey**

```d4
begin
    var EmpCursor : cursor := cursor (Employee capabilities {Updateable});
    var TempRow : row;
    TempRow = row {"Jed" Name,45 ID};
    EmpCursor.Insert(TempRow);
end;
```

**See Also**

[Using Cursors](../../../developers-guide/modeling-process-logic.md#using-cursors) \| [Cursor Capabilities](../../../developers-guide/d4-data-manipulation.md#cursor-capabilities) \| [System.FindKey](System.FindKey.md) \| [System.GetBookmark](System.GetBookmark.md)
