# System.iIn

**Declarations**

```d4
create operator .System.iIn(const AValue : generic, const AList : generic list) : System.Boolean
 class "System.ValueInListNode" static tags { DAE.IsBuiltin = "True" };
create operator .System.iIn(const AValue : System.Scalar, const ATable : generic table) : System.Boolean
 class "System.InTableNode" static tags { DAE.IsBuiltin = "True" };
create operator .System.iIn(const AValue : generic row, const ATable : generic table) : System.Boolean
 class "System.InTableNode" static tags { DAE.IsBuiltin = "True" };
```

**Remarks**

`in` is the parser recognized symbol for `iIn`. The `in` table/row/list operator is a binary operator that checks for the existence of a particular value in the source. The check can be for a row in a table, a value in a list, or a value in a table consisting of a single column. If the value is in the source, then the operator returns true, if it is not in the source, then the operator returns false.

**Using System.iIn**

```d4
create table Test in Temp
{
 ID : Integer,
 Direction : String,
 key {ID}
};
insert table
{
 row { 1 ID, 'North' Direction },
 row { 2, 'South' },
 row { 3, 'East' },
 row { 4, 'West' }
} into Test;
select 'South' in (Test over { Direction });
select row { 2 ID, 'South' Direction } in Test;
drop table Test;
```

```text
True
True
```

**See Also**

[Operator Precedence](../../../developers-guide/d4-language-elements.md#operator-precedence) \| [Table Operators](../../../developers-guide/d4-table-operators.md)

## System.iIn (List)

**Remarks**

`in` is the parser recognized symbol for the membership operator `iIn`. The `in` operator is a table, row, and list operator.

**See**

[System.iIn table operator](System.iIn.md)

## System.iIn (Row)

**Remarks**

`in` is the parser recognized symbol for `iIn`. The `in` operator is a table, row, and list operator.

**See**

[System.iIn table operator](System.iIn.md)
