# System.iAnd

**Declarations**

```d4
create operator .System.iAnd(const ALeftValue : System.Boolean, const ARightValue : System.Boolean) : System.Boolean
 class "System.BooleanAndNode" static tags { DAE.IsBuiltin = "True" };
```

**Remarks**

`and` is the parser recognized symbol for `iAnd`. The `and` operator is a logical and. Both of the operands must be true for the result to be true.

> [!NOTE]
> Dataphor does not do short-circuit evaluation.

The following table displays the results of evaluating `and`:

| `and`   | `true`  | `nil`   | `false` |
|---------|---------|---------|---------|
| `true`  | `true`  | `nil`   | `false` |
| `nil`   | `nil`   | `nil`   | `false` |
| `false` | `false` | `false` | `false` |

**Using System.iAnd**

```d4
select Manager where (Manager_ID % 2 == Employee_ID % 2) and (Employee_ID > 2);
```

**See Also**

[Operator Precedence](../../../developers-guide/d4-language-elements.md#operator-precedence)
