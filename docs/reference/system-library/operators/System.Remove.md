# System.Remove

**Declarations**

```d4
create operator .System.Remove(var AList : generic list, const AValue : generic)
 class "System.ListRemoveNode" static tags { DAE.IsFunctional = "False" };
create operator .System.Remove(const AString : System.String, const AStartIndex : System.Integer, const ALength : System.Integer) : System.String
 class "System.StringRemoveNode";
```

**Remarks**

As a string operator, `Remove` removes `ALength` characters from `AString` starting at `AStartIndex`.

As a list operator, `Remove` removes `AValue` from `AList`. If the item is not in the list then an exception is thrown.

> [!NOTE]
> String and list indexes are zero based.

**Using System.Remove**

```d4
create table Test in Temp { TestString : String };
begin
 var CurrString : String;
 var CurrList : list(String) :=
 {
  "Michelle",
  "Melinda",
  "Melissa",
  "Melody",
  "Margaret"
 };
 CurrList.Remove("Michelle");
 CurrString :=
  CurrList[0] + " " + CurrList[1]
  + " " + CurrList[2] + " " + CurrList[3];
 insert table { row { CurrString TestString } } into Test;
 CurrString := CurrString.Remove(7, 8);
 insert table { row { CurrString TestString } } into Test;
end;
select Test;
drop table Test;
```

```text
TestString
-------------------------------
Melinda Melissa Melody Margaret
Melinda Melody Margaret
```

**See Also**

[System.RemoveAt](System.RemoveAt.md) \| [System.Insert](System.Insert.md) \| [Using Lists](../../../developers-guide/modeling-process-logic.md#using-lists)

## System.Remove (String)

**Remarks**

`Remove` is a list and a string operator. See [System.Remove](System.RemoveAt.md).
