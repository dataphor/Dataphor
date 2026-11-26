# System.Concat

**Declarations**

```d4
create operator .System.Concat(const AStrings : list(System.String)) : System.String
 class "System.StringConcatNode";
create operator .System.Concat(const AStrings : list(System.String), const ADelimiter : System.String) : System.String
 class "System.StringConcatNode";
create aggregate operator .System.Concat(AString : System.String) : System.String
initialization
 class "System.EmptyFinalizationNode"
aggregation
begin
    if not(.System.IsNil(.AString)) then
        .result := (.System.IfNil(.result, '') + .AString);
end
finalization
 class "System.EmptyFinalizationNode" static tags { DAE.IsNilable = "True", DAE.IsOrderDependent = "True" };
create aggregate operator .System.Concat(AString : System.String, ADelimiter : System.String) : System.String
initialization
 class "System.EmptyFinalizationNode"
aggregation
begin
    if not(.System.IsNil(.AString)) then
        if (.System.Length(.result) > 0) then
            .result := ((.result + .System.IfNil(.ADelimiter, '')) + .AString)
        else
            .result := (.System.IfNil(.result, '') + .AString);
end
finalization
 class "System.EmptyFinalizationNode" static tags { DAE.IsNilable = "True", DAE.IsOrderDependent = "True" };
```

**Remarks**

`Concat` can be invoked as a standard or an aggregate operator. It is used to concatinate strings together into a single string.

`Concat` as a standard operator takes a `List` of type `String` as an argument. An optional delimiter can also be passed as a `String` argument. It returns a string that is the concatination of the strings in the list.

`Concat` as an aggregate operator takes an optional delimiter as a `String` argument. It returns a string that is the concatination of all the source string from each row.

> [!NOTE]
> Concatination of an empy list of strings returns an empty string.

**Using System.Concat**

```d4
create session table ConcatExample
{
 TheString : String,
 key{TheString}
};
begin
 var LList : list(String) := { "Concat" };
 LList.Add("Operator");
 LList.Insert("Standard", 0);
 
 ConcatExample :=
 table
  {
   row { LList.Concat(" ") TheString },
   row { LList.Concat() TheString }
  };
end;
select ConcatExample;

ConcatExample :=
 table
 {
  row { "Aggregate" TheString },
  row { "ConCat" },
  row { "Operator" }
 };
select ConcatExample group add { Concat(TheString) NoDelimiter };
select ConcatExample add { " " Delimiter } group add { Concat(TheString, Delimiter) WithDelimiter };

insert table { row { "Standalone invocation" TheString } } into ConcatExample;
select Concat( { TheString, Space } from ConcatExample add { ", " Space });

drop table ConcatExample;
```

```text
TheString
------------------------
Standard Concat Operator
StandardConcatOperator

NoDelimiter
-----------------------
AggregateConCatOperator

WithDelimiter
-------------------------
Aggregate ConCat Operator

Aggregate, ConCat, Operator, Standalone invocation
```

**See Also**

[System.Split](System.Split.md) \| [Aggregate Operators](../../../developers-guide/d4-table-operators.md#aggregate) \| [System.String Scalar Type](../types/System.String.md)

## System.Concat (Aggregate)

**Remarks**

`Concat` is an aggregate and a string operator.

**See**

[System.Concat String Operator](System.Concat.md)
