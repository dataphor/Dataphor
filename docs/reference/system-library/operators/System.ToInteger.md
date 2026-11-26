# System.ToInteger

**Declarations**

```d4
create operator .System.ToInteger(const AValue : System.Boolean) : System.Integer
 class "System.BooleanToIntegerNode" static tags { DAE.IsOrderPreserving = "True" };
create operator .System.ToInteger(const AValue : System.String) : System.Integer
 class "System.StringToIntegerNode";
create operator .System.ToInteger(const AValue : System.Decimal) : System.Integer
 class "System.DecimalToIntegerNode" static tags { DAE.IsOrderPreserving = "True" };
create operator .System.ToInteger(const AValue : System.Money) : System.Integer
 class "System.DecimalToIntegerNode" static tags { DAE.IsOrderPreserving = "True" };
create operator .System.ToInteger(const AValue : System.Byte) : System.Integer
 class "System.ByteToIntegerNode" static tags { DAE.IsOrderPreserving = "True" };
create operator .System.ToInteger(const AValue : System.Short) : System.Integer
 class "System.ShortToIntegerNode" static tags { DAE.IsOrderPreserving = "True" };
create operator .System.ToInteger(const AValue : System.Long) : System.Integer
 class "System.LongToIntegerNode" static tags { DAE.IsOrderPreserving = "True" };
```

**Remarks**

`Boolean`, `Byte`, `Decimal`, `Long`, `Money`, `Short`, and `String` types can be explicitly converted to `Integer` using `ToInteger`.

With a `Boolean`, `true` will return 1, `false` will return 0.

With strings, converts the specified `String` representation of a number, with optional sign, to an equivalent signed 32-bit integer.

An error will occur if the value is outside that range of an `Integer` (-2,147,483,648 to +2,147,483,647).

**Using System.ToInteger**

```d4
select ToInteger(Long(1234));
select ToInteger("-4567");
select ToInteger(false);
```

```text
1234
-4567
0
```

**See Also**

[System Data Types](../../../developers-guide/d4-language-elements.md#system-data-types) \| [System.ToBoolean](System.ToBoolean.md) \| [System.ToByte](System.ToByte.md) \| [System.ToDate](System.ToDate.md) \| [System.ToDateTime](System.ToDateTime.md) \| [System.ToDecimal](System.ToDecimal.md) \| [System.ToGuid](System.ToGuid.md) \| [System.ToLong](System.ToLong.md) \| [System.ToMoney](System.ToMoney.md) \| [System.ToShort](System.ToShort.md) \| [System.ToString](System.ToString.md) \| [System.ToTime](System.ToTime.md) \| [System.ToTimeSpan](System.ToTimeSpan.md) \| [System.ToVersionNumber](System.ToVersionNumber.md)
