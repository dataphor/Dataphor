# System.ToLong

**Declarations**

```d4
create operator .System.ToLong(const AValue : System.Boolean) : System.Long
 class "System.BooleanToLongNode" static tags { DAE.IsOrderPreserving = "True" };
create operator .System.ToLong(const AValue : System.String) : System.Long
 class "System.StringToLongNode";
create operator .System.ToLong(const AValue : System.Byte) : System.Long
 class "System.ByteToLongNode" static tags { DAE.IsOrderPreserving = "True" };
create operator .System.ToLong(const AValue : System.Short) : System.Long
 class "System.ShortToLongNode" static tags { DAE.IsOrderPreserving = "True" };
create operator .System.ToLong(const AValue : System.Integer) : System.Long
 class "System.IntegerToLongNode" static tags { DAE.IsOrderPreserving = "True" };
create operator .System.ToLong(const AValue : System.Decimal) : System.Long
 class "System.DecimalToLongNode" static tags { DAE.IsOrderPreserving = "True" };
create operator .System.ToLong(const AValue : System.Money) : System.Long
 class "System.DecimalToLongNode" static tags { DAE.IsOrderPreserving = "True" };
```

**Remarks**

`Boolean`, `Byte`, `Decimal`, `Integer`, `Money`, `Short`, and `String` types can be explicitly converted to `Long` using `ToLong`.

With a `Boolean`, `true` will return 1, `false` will return 0.

With strings, converts the specified `String` representation of a number, with optional sign, to an equivalent signed 64-bit integer.

An error will occur if the value is outside that range of an `Long` (-9,223,372,036,854,775,808 to +9,223,372,036,854,775,807).

**Using System.ToLong**

```d4
select ToLong(Integer(1234));
select ToLong("-4567");
select ToInteger(false);
```

```text
1234
-4567
0
```

**See Also**

[System Data Types](../../../developers-guide/d4-language-elements.md#system-data-types) \| [System.ToBoolean](System.ToBoolean.md) \| [System.ToByte](System.ToByte.md) \| [System.ToDate](System.ToDate.md) \| [System.ToDateTime](System.ToDateTime.md) \| [System.ToDecimal](System.ToDecimal.md) \| [System.ToGuid](System.ToGuid.md) \| [System.ToInteger](System.ToInteger.md) \| [System.ToMoney](System.ToMoney.md) \| [System.ToShort](System.ToShort.md) \| [System.ToString](System.ToString.md) \| [System.ToTime](System.ToTime.md) \| [System.ToTimeSpan](System.ToTimeSpan.md) \| [System.ToVersionNumber](System.ToVersionNumber.md)
