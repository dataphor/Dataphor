# System.iPower

**Declarations**

```d4
create operator .System.iPower(const ALeftValue : System.Decimal, const ARightValue : System.Decimal) : System.Decimal
 class "System.DecimalPowerNode" static tags { DAE.IsBuiltin = "True" };
```

**Remarks**

`**` is the parser recognized symbol for `iPower`. This is a binary operator that takes the left value and raises it to the power of the right value.

**Using \*\***

```d4
select Manager add { Manager_ID ** Employee_ID Power };
```

```text
Manager_ID Employee_ID Power 
---------- ----------- ----- 
1          2           1     
1          3           1     
2          4           16    
2          6           64    
4          5           1024
```

**See Also**

[Operator Precedence](../../../developers-guide/d4-language-elements.md#operator-precedence)
