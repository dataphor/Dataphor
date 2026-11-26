# System.ToBoolean

**Declarations**

```d4
create operator .System.ToBoolean(const AValue : System.String) : System.Boolean
 class "System.StringToBooleanNode";
create operator .System.ToBoolean(const AValue : System.Byte) : System.Boolean
 class "System.ByteToBooleanNode";
create operator .System.ToBoolean(const AValue : System.Short) : System.Boolean
 class "System.ShortToBooleanNode";
create operator .System.ToBoolean(const AValue : System.Integer) : System.Boolean
 class "System.IntegerToBooleanNode";
create operator .System.ToBoolean(const AValue : System.Long) : System.Boolean
 class "System.LongToBooleanNode";
```

**Remarks**

`Byte`, `Integer`, `Long`, `Short`, and `String` types can be explicitly converted to `Boolean` using `ToBoolean`.

With a numeric value, 0 will return as `false`, any other value will return as `true`.

With strings, a case-insensitive compare is done. The string "true" will return true, the string "false" will return false. An error will occur if the string is not one of the two.

**Using System.ToBoolean**

```d4
select ToBoolean(-1234);
select ToBoolean("FALSE");
```

```text
True
False
```

**See Also**

[System Data Types](../../../developers-guide/d4-language-elements.md#system-data-types) \| [System.ToByte](System.ToByte.md) \| [System.ToDate](System.ToDate.md) \| [System.ToDateTime](System.ToDateTime.md) \| [System.ToDecimal](System.ToDecimal.md) \| [System.ToGuid](System.ToGuid.md) \| [System.ToInteger](System.ToInteger.md) \| [System.ToLong](System.ToLong.md) \| [System.ToMoney](System.ToMoney.md) \| [System.ToShort](System.ToShort.md) \| [System.ToString](System.ToString.md) \| [System.ToTime](System.ToTime.md) \| [System.ToTimeSpan](System.ToTimeSpan.md) \| [System.ToVersionNumber](System.ToVersionNumber.md)
