# System.iExists

**Declarations**

```d4
create operator .System.iExists(ATable : generic table) : System.Boolean
 class "System.ExistsNode" static tags { DAE.IsBuiltin = "True" };
```

**Remarks**

`exists` is the parser recognized symbol for `iExists`. The `exists` operator returns true if the table specified has any rows in it.

**Using System.iExists**

```d4
if (not exists Employee) then
begin
    insert table { row { "Jed" Name, 1 ID } } into Employee;
end
else
begin
    insert table { row { "Jed" Name, 42 ID } } into Employee;
end;
select Employee;
```

```text
Statement Executed Successfully!

ID Name 
-- ---- 
1  Joe  
2  John 
3  Josh 
4  Jeff 
5  Jake 
6  Jeff 
42 Jed
```

**See Also**

[Operator Precedence](../../../developers-guide/d4-language-elements.md#operator-precedence)
