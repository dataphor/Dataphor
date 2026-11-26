# System.Change

**Declarations**

```d4
create operator .System.Change(const ACursor : generic cursor, const AOldRow : generic row, var ANewRow : generic row) : System.Boolean
 class "System.CursorChangeNode" static tags { DAE.IsFunctional = "False" };
create operator .System.Change(const ACursor : generic cursor, const AOldRow : generic row, var ANewRow : generic row, const AColumnName : System.String) : System.Boolean
 class "System.CursorChangeNode" static tags { DAE.IsFunctional = "False" };
```

**Remarks**

`Change` is a cursor level operator. It allows access to the proposable calls for the cursor. It permits a "what if" check.

By passing in the proposed row change in `ARow`, the results that would occur if the change were made are passed back in `ARow`.

If `AColumnName` is specified, only that column is involved in the proposal. If `AColumnName` is omitted, all columns are involved.

The boolean value returned is `true` if a change occured to any of the columns in the row, `false` if no change occured.

**Using System.Change**

```d4
CreateLibrary
(
 LibraryDescriptor
 (
  "Lib",
  VersionNumberUndefined(),
  "",
  list(FileReference) {},
  list(LibraryReference) { LibraryReference("System",VersionNumberUndefined()) }
 )
);
RegisterLibrary("Lib");
EnsureGenerators();
create table Table in Temp
{
 ID : Integer { default GetNextGenerator("Table.ID") },
 Price : Money { default $10.00 },
 Quantity : Integer
 {
  constraint QValue value > 0
   tags
   {
    DAE.SimpleMessage = "Quantity must be greater than zero"
   }
 },
 key { ID }
};
create view Extended    Table add { Price * Quantity Total };

create operator TryChange() : typeof(row from Extended)
begin
 var LCursor := cursor(Extended);
 try
  if LCursor.Next() then
   begin   result := LCursor.Select();
    update result set { Price := $25.10, Quantity := 4 };
    LCursor.Change(var result);
   end;
 finally
  LCursor.Close();
 end;
end;

create operator TryDefault() : typeof(row from (Extended))
begin
 var LCursor := cursor(Extended);
 try
  result := row { nil ID, nil Price, 1 Quantity, nil Total };
  LCursor.Default(var result);
 finally
  LCursor.Close();
 end;
end;

create operator TryValidate(const AQuantity : Integer) : String
begin
 var LCursor := cursor(Extended);
 var LRow : typeof(row from(Extended));
 try
  LRow := row { nil ID, nil Price, AQuantity Quantity, nil Total };
  try
   result :=
    if LCursor.Validate(var LRow)
    then ""
    else "Validation OK";
  except
   on E : Error do
   begin
    result := E.Message;
   end;
  end;
 finally
  LCursor.Close();
 end;
end;

insert table { row { $14.21 Price, 5 Quantity } } into Table;
select "** Change operator **";
select table { TryChange() };
select "** Default operator **";
select table { TryDefault() };
select "** Validate operator **";
select TryValidate(-1);
select TryValidate(1);
select "What is really in the table?";
select Extended;

drop operator TryValidate(Integer);
drop operator TryDefault();
drop operator TryChange();
drop view Extended;
drop table Table;
UnregisterLibrary("Lib");
DropLibrary("Lib");
```

```d4
** Change operator **
ID Price  Quantity Total
-- ------ -------- -------
1  $25.10 4        $100.40

** Default operator **
ID Price  Quantity Total
-- ------ -------- ------
2  $10.00 1        $10.00

** Validate operator **
Quantity must be greater than zero.
Validation OK

What is really in the table?
ID Price  Quantity Total
-- ------ -------- ------
1  $14.21 5        $71.05
```

**See Also**

[System.Change](System.Change.md) \| [System.Default](System.Default.md)
