# System.ToByte

**Declarations**

```d4
create operator .System.ToByte(const AValue : System.Boolean) : System.Byte
 class "System.BooleanToByteNode" static tags { DAE.IsOrderPreserving = "True" };
create operator .System.ToByte(const AValue : System.String) : System.Byte
 class "System.StringToByteNode";
create operator .System.ToByte(const AValue : System.Short) : System.Byte
 class "System.ShortToByteNode" static tags { DAE.IsOrderPreserving = "True" };
create operator .System.ToByte(const AValue : System.Integer) : System.Byte
 class "System.IntegerToByteNode" static tags { DAE.IsOrderPreserving = "True" };
create operator .System.ToByte(const AValue : System.Long) : System.Byte
 class "System.LongToByteNode" static tags { DAE.IsOrderPreserving = "True" };
```

**Remarks**

`Boolean`, `Integer`, `Long`, `Short`, and `String` types can be explicitly converted to `Byte` using `ToByte`.

With a `Boolean`, `true` will return 1, `false` will return 0.

With strings, converts the specified `String` representation of a number, with optional plus sign, to an equivalent 8-bit unsigned integer.

An error will occur if the value is outside that range of a Byte (0 to +255).

**Using System.ToByte**

```d4
select ToByte(Integer(200));
select ToByte("+87");
select ToByte(true);
```

```text
200
87
1
```

**See Also**

[System Data Types](../../../developers-guide/d4-language-elements.md#system-data-types) \| [System.ToBoolean](System.ToBoolean.md) \| [System.ToDate](System.ToDate.md) \| [System.ToDateTime](System.ToDateTime.md) \| [System.ToDecimal](System.ToDecimal.md) \| [System.ToGuid](System.ToGuid.md) \| [System.ToInteger](System.ToInteger.md) \| [System.ToLong](System.ToLong.md) \| [System.ToMoney](System.ToMoney.md) \| [System.ToShort](System.ToShort.md) \| [System.ToString](System.ToString.md) \| [System.ToTime](System.ToTime.md) \| [System.ToTimeSpan](System.ToTimeSpan.md) \| [System.ToVersionNumber](System.ToVersionNumber.md)
