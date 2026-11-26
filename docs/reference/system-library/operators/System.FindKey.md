# System.FindKey

**Declarations**

```d4
create operator .System.FindKey(const ACursor : generic cursor, const ARow : generic row) : System.Boolean
 class "System.CursorFindKeyNode";
```

**Remarks**

`FindKey` takes the cursor to the specified key. If the row doesn't exist, then this operator returns false, and it leaves the cursor where it was before the operator call. If the key is found, then it returns true and moves the cursor to that row. This key is either one recieved from `GetKey`, or one made. The key data specified must come from columns in the order clause in the table definition. This operator first checks to see if the first column in the order specification is in the row provided, then the second column in the order, and so on.

**See Also**

[Using Cursors](../../../developers-guide/modeling-process-logic.md#using-cursors) \| [Cursor Capabilities](../../../developers-guide/d4-data-manipulation.md#cursor-capabilities) \| [System.GotoBookmark](System.GotoBookmark.md) \| [System.GetKey](System.GetKey.md) \| [System.FindNearest](System.FindNearest.md)
