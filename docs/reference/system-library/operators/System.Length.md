# System.Length

**Declarations**

```d4
create operator .System.Length(const ABinary : System.Binary) : System.Long
 class "System.BinaryLengthNode";
create operator .System.Length(const AGraphic : System.Graphic) : System.Long
 class "System.BinaryLengthNode";
create operator .System.Length(const AString : System.String) : System.Integer
 class "System.StringLengthNode";
```

**Remarks**

`Length` returns the length of the specified string.

**Using System.Length**

```d4
select Employee add { Name.Length() NameLength};
```

```text
ID Name NameLength
-- ---- ----------
1  Joe  3
2  John 4
3  Josh 4
4  Jeff 4
5  Jake 4
6  Jeff 4
```

**See Also**

[System.String Scalar Type](../types/System.String.md)
