# System.iNot

**Declarations**

```d4
create operator .System.iNot(const AValue : System.Boolean) : System.Boolean
 class "System.BooleanNotNode" static tags { DAE.IsBuiltin = "True" };
```

**Remarks**

`not` if the parser recognized symbol for `iNot`. This is a unary operator that returns the logical negation of the given value.

The following table displays the results of evaluating `not`:

| Argument | Result  |
|----------|---------|
| `true`   | `false` |
| `nil`    | `nil`   |
| `false`  | `true`  |

**Using System.iNot**

```d4
select Employee where not( ID in (Manager over {Manager_ID}));
```

```text
ID Name 
-- ---- 
3  Josh 
5  Jake 
6  Jeff
```

**See Also**

[Operator Precedence](../../../developers-guide/d4-language-elements.md#operator-precedence)
