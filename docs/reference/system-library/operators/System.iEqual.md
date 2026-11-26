# System.iEqual

**Declarations**

```d4
create operator .System.iEqual(const ALeftValue : System.Byte, const ARightValue : System.Byte) : System.Boolean
 class "System.ByteEqualNode" static tags { DAE.IsBuiltin = "True" };
create operator .System.iEqual(const ALeftValue : System.Short, const ARightValue : System.Short) : System.Boolean
 class "System.ShortEqualNode" static tags { DAE.IsBuiltin = "True" };
create operator .System.iEqual(const ALeftValue : System.Integer, const ARightValue : System.Integer) : System.Boolean
 class "System.IntegerEqualNode" static tags { DAE.IsBuiltin = "True" };
create operator .System.iEqual(const ALeftValue : System.Long, const ARightValue : System.Long) : System.Boolean
 class "System.LongEqualNode" static tags { DAE.IsBuiltin = "True" };
create operator .System.iEqual(const ALeftValue : System.Boolean, const ARightValue : System.Boolean) : System.Boolean
 class "System.BooleanEqualNode" static tags { DAE.IsBuiltin = "True" };
create operator .System.iEqual(const ALeftValue : System.Guid, const ARightValue : System.Guid) : System.Boolean
 class "System.GuidEqualNode" static tags { DAE.IsBuiltin = "True" };
create operator .System.iEqual(const ALeftValue : System.Decimal, const ARightValue : System.Decimal) : System.Boolean
 class "System.DecimalEqualNode" static tags { DAE.IsBuiltin = "True" };
create operator .System.iEqual(const ALeftValue : System.String, const ARightValue : System.String) : System.Boolean
 class "System.StringEqualNode" static tags { DAE.IsBuiltin = "True" };
create operator .System.iEqual(const ALeftValue : System.Money, const ARightValue : System.Money) : System.Boolean
 class "System.MoneyEqualNode" static tags { DAE.IsBuiltin = "True" };
create operator .System.iEqual(const ALeftValue : System.TimeSpan, const ARightValue : System.TimeSpan) : System.Boolean
 class "System.TimeSpanEqualNode" static tags { DAE.IsBuiltin = "True" };
create operator .System.iEqual(const ALeftValue : System.DateTime, const ARightValue : System.DateTime) : System.Boolean
 class "System.DateTimeEqualNode" static tags { DAE.IsBuiltin = "True" };
create operator .System.iEqual(const ALeftValue : System.Date, const ARightValue : System.Date) : System.Boolean
 class "System.DateEqualNode" static tags { DAE.IsBuiltin = "True" };
create operator .System.iEqual(const ALeftValue : System.Time, const ARightValue : System.Time) : System.Boolean
 class "System.TimeEqualNode" static tags { DAE.IsBuiltin = "True" };
create operator .System.iEqual(const ALeftValue : System.Error, const ARightValue : System.Error) : System.Boolean
 class "System.ErrorEqualNode" static tags { DAE.IsBuiltin = "True" };
create operator .System.iEqual(const ALeftValue : System.Binary, const ARightValue : System.Binary) : System.Boolean
 class "System.BinaryEqualNode" static tags { DAE.IsBuiltin = "True" };
create operator .System.iEqual(const ALeftValue : System.Graphic, const ARightValue : System.Graphic) : System.Boolean
 class "System.BinaryEqualNode" static tags { DAE.IsBuilting = "True" };
create operator .System.iEqual(const ALeftValue : System.Name, const ARightValue : System.Name) : System.Boolean
 class "System.StringEqualNode" static tags { DAE.IsBuiltin = "True" };
create operator .System.iEqual(const ALeftValue : generic row, const ARightValue : generic row) : System.Boolean
 class "System.RowEqualNode" static tags { DAE.IsBuiltin = "True" };
create operator .System.iEqual(ALeftValue : generic table, ARightValue : generic table) : System.Boolean
 class "System.TableEqualNode" static tags { DAE.IsBuiltin = "True" };
create operator .System.iEqual(ALeftValue : generic list, ARightValue : generic list) : System.Boolean
 class "System.ListEqualNode" static tags { DAE.IsBuiltin = "True" };
create operator .System.iEqual(const ALeftValue : System.UserID, const ARightValue : System.UserID) : System.Boolean
 class "System.StringEqualNode" tags { DAE.IsBuiltin = "True" };
create operator .System.iEqual(const ALeftValue : System.VersionNumber, const ARightValue : System.VersionNumber) : System.Boolean
 class "System.VersionNumberEqualNode";
create operator .System.iEqual(const ALeftValue : System.FileReference, const ARightValue : System.FileReference) : System.Boolean
 class "System.FileReferenceEqualNode" static tags { DAE.IsBuiltin = "True" };
create operator .System.iEqual(const ALeftValue : System.LibraryDescriptor, const ARightValue : System.LibraryDescriptor) : System.Boolean
 class "System.SystemLibraryDescriptorEqualNode" static tags { DAE.IsBuiltin = "True" };
```

**Remarks**

`=` is the parser recognized symbol for `iEqual`. The `Equal` operator is a comparison operator that checks to see if the two values on either side of the operator are equal. This operator returns a boolean value representing the result of the comparison.

**Using System.iEqual**

```d4
select EmployeePhone where Phone.Length() = 8 add {ID.ToString() = Phone bool};
```

```text
ID Phone    bool  
-- -------- ----- 
1  555-1000 False 
1  555-1234 False 
2  555-4321 False 
4  555-4444 False 
5  555-5678 False 
```

**See Also**

[Operator Precedence](../../../developers-guide/d4-language-elements.md#operator-precedence) \| [Table Comparison Operators](../../../developers-guide/d4-table-operators.md#comparison-operators)
