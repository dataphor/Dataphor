# System.ToShort

**Declarations**

```d4
create operator .System.ToShort(const AValue : System.Boolean) : System.Short
 class "System.BooleanToShortNode" static tags { DAE.IsOrderPreserving = "True" };
create operator .System.ToShort(const AValue : System.String) : System.Short
 class "System.StringToShortNode";
create operator .System.ToShort(const AValue : System.Byte) : System.Short
 class "System.ByteToShortNode" static tags { DAE.IsOrderPreserving = "True" };
create operator .System.ToShort(const AValue : System.Integer) : System.Short
 class "System.IntegerToShortNode" static tags { DAE.IsOrderPreserving = "True" };
create operator .System.ToShort(const AValue : System.Long) : System.Short
 class "System.LongToShortNode" static tags { DAE.IsOrderPreserving = "True" };
```

**Remarks**

`Boolean`, `Byte`, `Integer`, `Long`, and `String` types can be explicitly converted to `Short` using `ToShort`.

With a `Boolean`, `true` will return 1, `false` will return 0.

With strings, converts the specified `String` representation of a number, with optional sign, to an equivalent signed 16-bit integer.

An error will occur if the value is outside that range of an `Short` (-32768 to +32767).

**Using System.ToShort**

```d4
select ToShort(Integer(1234));
select ToShort("-4567");
select ToShort(false);
```

```text
1234
-4567
0
```

**See Also**

[System Data Types](../../../developers-guide/d4-language-elements.md#system-data-types) \| [System.ToBoolean](System.ToBoolean.md) \| [System.ToByte](System.ToByte.md) \| [System.ToDate](System.ToDate.md) \| [System.ToDateTime](System.ToDateTime.md) \| [System.ToDecimal](System.ToDecimal.md) \| [System.ToGuid](System.ToGuid.md) \| [System.ToInteger](System.ToInteger.md) \| [System.ToLong](System.ToLong.md) \| [System.ToMoney](System.ToMoney.md) \| [System.ToString](System.ToString.md) \| [System.ToTime](System.ToTime.md) \| [System.ToTimeSpan](System.ToTimeSpan.md) \| [System.ToVersionNumber](System.ToVersionNumber.md)
