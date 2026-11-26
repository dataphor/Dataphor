# System.iOr

**Declarations**

```d4
create operator .System.iOr(const ALeftValue : System.Boolean, const ARightValue : System.Boolean) : System.Boolean
 class "System.BooleanOrNode" static tags { DAE.IsBuiltin = "True" };
```

**Remarks**

`or` is the parser recognized symbol for `iOr`. The `or` operator is a logical or. Both of the operands must be false for the result to be false.

> [!NOTE]
> D4 does not do short-circuit evaluation.

The following table displays the results of evaluating `or`:

| `or`    | `true` | `nil`  | `false` |
|---------|--------|--------|---------|
| `true`  | `true` | `true` | `true`  |
| `nil`   | `true` | `nil`  | `nil`   |
| `false` | `true` | `nil`  | `false` |

**Using System.iOr**

```d4
select Manager where (Manager_ID = 1 or Manager_ID = 2);
```

```text
Manager_ID Employee_ID 
---------- ----------- 
1          2           
1          3           
2          4           
2          6
```

**See Also**

[Operator Precedence](../../../developers-guide/d4-language-elements.md#operator-precedence)
