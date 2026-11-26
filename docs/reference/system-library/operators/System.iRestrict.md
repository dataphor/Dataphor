# System.iRestrict

**Declarations**

```d4
create operator .System.iRestrict(ATable : generic table, const ACondition : System.Boolean) : generic table
 class "System.RestrictNode" static tags { DAE.IsBuiltin = "True" };
```

**Remarks**

`where` is the parser recognized symbol for the restriction operator `iRestrict`. For a complete description of this operator, refer to the [Restriction](../../../developers-guide/d4-table-operators.md#restriction) discussion in the D4 Language Guide.

**See Also**

[Operator Precedence](../../../developers-guide/d4-language-elements.md#operator-precedence) \| [Restriction](../../../developers-guide/d4-table-operators.md#restriction)
