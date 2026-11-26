# System.FindNearest

**Declarations**

```d4
create operator .System.FindNearest(const ACursor : generic cursor, const ARow : generic row)
 class "System.CursorFindNearestNode";
```

**Remarks**

`FindNearest` takes the cursor to the specified key. If the row doesn't exist then it will attempt to move the cursor to the row closest to the specified row. This key is either one recieved from `GetKey`, or one made. The key data specified must come from columns in the order clause in the table definition. This operator first checks to see if the first column in the order specification is in the row provided, then the second column in the order, and so on.

**See Also**

[Using Cursors](../../../developers-guide/modeling-process-logic.md#using-cursors) \| [Cursor Capabilities](../../../developers-guide/d4-data-manipulation.md#cursor-capabilities) \| [System.FindKey](System.FindKey.md)
