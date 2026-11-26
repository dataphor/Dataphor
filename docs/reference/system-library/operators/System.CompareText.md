# System.CompareText

**Declarations**

```d4
create operator .System.CompareText(const ALeftString : System.String, const ARightString : System.String) : System.Integer
 class "System.StringCompareTextNode";
```

**Remarks**

`CompareText` performs a case insensitive compare of two strings. If the left string compares as less than the right string -1 is returned. If the strings are compared as equal 0 is returned. If the left string is compared as greater than the right string 1 is returned.

**Using System.CompareText**

```d4
select CompareText("ABC", "bcd");
select CompareText("abc", "ABC");
select "BCD".CompareText("abc");
```

```text
-1
0
1
```

**See Also**

[System.iEqual (`=`)](System.iEqual.md) \| [System.iCompare (`?=`)](System.iCompare.md) \| [System.String Scalar Type](../types/System.String.md)
