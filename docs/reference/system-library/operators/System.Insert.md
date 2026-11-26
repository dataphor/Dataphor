# System.Insert

**Declarations**

```d4
create operator .System.Insert(var AList : generic list, AValue : generic, const AIndex : System.Integer)
 class "System.ListInsertNode" static tags { DAE.IsFunctional = "False" };
create operator .System.Insert(const AString : System.String, const AStartIndex : System.Integer, const AInsertString : System.String) : System.String
 class "System.StringInsertNode";
create operator .System.Insert(const ACursor : generic cursor, const ARow : generic row)
 class "System.CursorInsertNode" static tags { DAE.IsFunctional = "False" };
```

**Remarks**

As a cursor operator, `Insert` inserts `ARow` into `ACursor`.

As a list operator, `Insert` inserts `AValue` into `AList` at location `AIndex`.

As a String operator, `Insert` inserts `AInsertString` into `AString` starting at location `AStartIndex`.

> [!NOTE]
> String and list indexes are zero based.

**Using System.Insert**

```d4
create table Test in Temp { TestString : String };
begin
 var LTestString : String;
 var LTestList := list(String){ "Great" };
 var LTestCursor : cursor
  := cursor(Test capabilities { Navigable, Updateable } );
 LTestList.Insert("Dataphor", 0 );
 LTestString := LTestList[0] + LTestList[1];
 LTestString := LTestString.Insert(8, " is ");
 LTestCursor.Insert( row { LTestString TestString } );
 LTestCursor.Close();
end;
select Test;
drop table Test;
```

```text
TestString
-----------------
Dataphor is Great
```

**See Also**

[Using Cursors](../../../developers-guide/modeling-process-logic.md#using-cursors)

## System.Insert (List)

**Remarks**

[`System.Insert`](System.Insert.md) is a Cursor, List, and String operator. See [System.Insert](System.Insert.md).

## System.Insert (String)

**Remarks**

[`System.Insert`](System.Insert.md) is a Cursor, List, and String operator. See [System.Insert](System.Insert.md).
