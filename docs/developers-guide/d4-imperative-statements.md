# Imperative Statements

## Summary

This section covers the imperative statements available in the D4 language. These include variable declaration and assignment, flow control constructs, and exception handling statements.

Imperative statements in D4 have the following syntax:

```bnf
<imperative statement> ::=
    <block> |
    <expression> |
    <variable declaration statement> |
    <assignment statement> |
    <exit statement> |
    <if statement> |
    <while statement> |
    <do while statement> |
    <for statement> |
    <repeat statement> |
    <break statement> |
    <continue statement> |
    <case statement> |
    <try finally statement> |
    <try except statement> |
    <try commit statement> |
    <raise statement>
```

The following sections cover each statement in detail.

## Variables

There are two types of variables in D4, global, and local. Only table variables may be global, and are declared through the data definition language statements of D4. Local variables may be of any type, and are only valid for the scope in which they are declared. For more information on scoping, see the Blocks and Scoping section.

### Variable Declaration Statement

Variable declaration statements in D4 have the following syntax:

```bnf
<variable declaration statement> ::=
    var <ne variable definition commalist>

<variable definition> ::=
    <qualified identifier> [: <type specifier>] [:= <expression>]
```

Each \<variable definition\> includes an optional initialization expression which is the initial value for the variable. If no initialization expression is specified, the variable is assigned the default value for the type. If no default is specified, the variable is [nil](d4-language-elements.md#nil-semantics), or has no value.

The [\<type specifier](d4-language-elements.md#types)\> is optional if and only if an initialization expression is specified. In this case, the compiler uses the type of the initialization expression to determine the type of the variable.

### Assignment Statement

Assignment statements in D4 have the following syntax:

```bnf
<assignment statement> ::=
    <target> := <expression>

<target> ::=
    <expression>
```

The expression specified by \<target\> must be a valid assignment target. This includes properties of representations of scalar values, and updatable expressions like table expressions. The expression specified by [\<expression](d4-language-elements.md#expressions)\> must be *assignment-compatible* with the assignment target. For a definition of assignment-compatibility, refer to the [Operator Resolution](d4-catalog.md#operator-resolution) discussion.

## Blocks and Scoping

Blocks are the primary means of establishing scope in D4. A *scope* defines the lifetime of variables within D4. Globally scoped variables are persisted with the database state, while locally scoped variables are only available within the scope in which they are defined.

Local scopes may be nested to any level. Variables declared within a local scope must be uniquely named, but may hide variables declared in the global scope. Within a nested local scope, variables declared in parent scopes are visible.

The following constructs also establish scope within their respective blocks:

- [Do-while statement](#do-while-statement).
- [Repeat-until statement](#repeat-until-statement).
- [Try-except statement](#try-except-statement), in both the try block, and each exception handler.
- [Try-finally statements](#try-finally-statement), in both the try block, and the finally block.

Blocks in D4 have the following syntax:

```bnf
<block> ::=
    begin
        <terminated statement list>
    end
```

The following example illustrates the use of blocks in D4:

```d4
begin
    var I : Integer := 1;
    begin
        var J : Integer := 5;
        J := J + I; // I is visible;

        // Global variables are visible
        J := J + Count(System.Objects);
    end;
    I := I + 1; // J is out of scope
end;
```

## Flow Control

Flow control statements allow control over the execution in a D4 program. These include branching statements and looping statements.

### Exit Statement

The exit statement unconditionally exits an operator. Control passes immediately back to the caller. If an exit statement is used within a try-finally, the finally block is executed.

Exit statements in D4 have the following syntax:

```bnf
<exit statement> ::=
    exit
```

The following example illustrates the use of the exit statement:

```d4
create operator TestExit(var AValue : Integer)
begin
    if AValue < 0 then
        exit;

    /*
       this statement will not be executed
       if AValue is less than zero
    */
    AValue := AValue * 10;
end;
```

### Branching Statements

Branching statements allow for the conditional execution of different blocks of code based on a specified expression.

#### If Statement

The if statement allows statements to be executed conditionally.

If statements in D4 have the following syntax:

```bnf
<if statement> ::=
    if <expression> then
        <statement>
    [else <statement>]
```

The expression specified by [\<expression](d4-language-elements.md#expressions)\> must be boolean-valued. If the expression evaluates to true, the statement given by [\<statement](d4-language-elements.md#statements)\> is executed. The if statement may include an optional else clause which specifies a statement to be executed if the expression evaluates to false or nil. Note that if an else clause is specified, it is part of the if statement, so the statement separator ([;](d4-scripts-and-execution.md#summary)) is invalid before the **else** keyword.

The following example illustrates the use of the if statement:

```d4
if Count(System.Objects) = 0 then
    raise Error("No objects defined in the system catalog")
else
    raise Error("Count of objects in the system is " +
        Count(System.Objects).ToString());
```

**See Also**

[if expression](d4-language-elements.md#conditional-expression)

#### Case Statement

Case statements allow the execution of a specific statement based on a list of conditions. Case statements are a syntactic shorthand for an iterated if statement.

The case statement has two forms. One is based on an expression, one allows a conditional to be associated with each case item.

In the first form, the case statement is based on an expression. Each case statement item contains a single expression which is compared against the case expression. If the case item expression is equal to the case expression, the statement associated with the item is selected. Each case item is compared with the case expression until a match is found. If no match is found, the default case item is selected.

In the second form, the case statement is not based on an expression, rather, each case item contains a boolean-valued condition. The first case item for which the condition evaluates to true is selected. The default case item is selected if no case item expression evaluates to true.

If no case item is selected the statement is effectively a no-op. Once a statement has been selected, it is executed, and control passes to the first statement following the case statement.

Case statements in D4 have the following syntax:

```bnf
<case statement> ::=
    case [<expression>]
        <ne case statement item list>
        [else <terminated statement>]
    end

<case statement item> ::=
    when <expression> then <terminated statement>
```

The following examples illustrate the use of the case statement in D4:

```d4
create operator DigitToWords(const ADigit : Integer) : String
begin
    case ADigit
        when 0 then result := 'Zero';
        when 1 then result := 'One';
        when 2 then result := 'Two';
        when 3 then result := 'Three';
        when 4 then result := 'Four';
        when 5 then result := 'Five';
        when 6 then result := 'Six';
        when 7 then result := 'Seven';
        when 8 then result := 'Eight';
        when 9 then result := 'Nine';
        else
            raise Error("Digit out of range: " + ADigit.ToString());
    end;
end;
```

```d4
create operator DigitToWords(const ADigit : Integer) : String
begin
    case
        when ADigit = 0 then result := 'Zero';
        when ADigit = 1 then result := 'One';
        when ADigit = 2 then result := 'Two';
        when ADigit = 3 then result := 'Three';
        when ADigit = 4 then result := 'Four';
        when ADigit = 5 then result := 'Five';
        when ADigit = 6 then result := 'Six';
        when ADigit = 7 then result := 'Seven';
        when ADigit = 8 then result := 'Eight';
        when ADigit = 9 then result := 'Nine';
        else
            raise Error("Digit out of range: " + ADigit.ToString());
    end;
end;
```

**See Also**

[case expression](d4-language-elements.md#conditional-expression)

### Looping Statements

Looping statements allow for a given statement to be executed multiple times. There are four types of looping statements in D4: the for statement, the while statement, the do-while statement, and the repeat-until statement.

Note that all these looping constructs can be expressed in terms of a simple while loop. The variations are provided to allow the developer to build more readable statements, as each loop is appropriate for different scenarios.

#### For Statement

The for statement allows a given statement to be executed a specified number of times.

For statements in D4 have the following syntax:

```bnf
<for statement> ::=
    for
        [var] <qualified identifier> [: <type specifier>]
        := <expression> (to | downto) <expression>
        [step <expression>]
        do <statement>
```

The for statement uses an *iteration control variable*, or ICV, to control statement iteration. The ICV may be an existing variable within the scope, or it may be declared by specifying the **var** keyword prior to the variable name, or the type of the variable within the for statement. If the **var** keyword is specified, the type specifier is optional.

In all cases, the ICV must be initialized to the initial bound for the iteration. The final bound is then specified as well as the iteration direction. An optional step clause specifies the magnitude of the increment or decrement to the ICV. The for statement is a post-test loop, so the statement is run, then the ICV is tested against the final bound. The type of the variable used for the ICV must have addition or subtraction operators defined as appropriate, as well as the necessary comparison operators.

Note that the boundary condition will be evaluated on every iteration. If this condition has side-effects, this may result in unexpected behavior.

The following example illustrates the use of the for statement:

```d4
create table Data { ID : Integer, key { ID } };

for I : Integer := 10 to 100 step 10 do
    insert table { row { I ID } } into Data;

select Data;
```

```text
ID
---
10
20
30
40
50
60
70
80
90
100
```

**See Also**

[Looping Statements](#looping-statements) \| [Break and Continue](#break-and-continue)

#### While Statement

The while statement allows a given statement to be executed as long as a specified condition is true. The while statement is a pre-test loop, meaning that the condition is checked, and then the statement is executed.

While statements in D4 have the following syntax:

```bnf
<while statement> ::=
    while <expression> do <statement>
```

The expression specified by \<expression\> must be boolean-valued.

The following example illustrates the use of the while statement:

```d4
begin
    var I : Integer := 0;
    while I < 20 do
        I := I + 1;
end;
```

**See Also**

[Looping Statements](#looping-statements) \| [Break and Continue](#break-and-continue)

#### Do While Statement

The do-while statement allows a given block to be executed as long as a specified condition is true. The do-while statement is a post-test loop, meaning that the statement is executed, then the condition is tested.

Do while statements in D4 have the following syntax:

```bnf
<do while statement> ::=
    do <terminated statement list> while <expression>
```

The expression specified by [\<expression](d4-language-elements.md#expressions)\> must be boolean-valued.

The following example illustrates the use of the do-while statement:

```d4
begin
    var I : Integer := 0;
    do
        I := I + 1;
    while I < 20;
end;
```

**See Also**

[Looping Statements](#looping-statements) \| [Break and Continue](#break-and-continue)

#### Repeat Until Statement

The repeat-until statement allows a given block to be executed until a specified condition returns true. The repeat until is a post-test loop, meaning that the statement is executed, then the condition is tested.

Repeat until statements in D4 have the following syntax:

```bnf
<repeat statement> ::=
    repeat <terminated statement list> until <condition>

<condition> ::=
    <expression>
```

The expression given by \<condition\> must be boolean-valued.

The following example illustrates the use of the repeat-until statement:

```d4
begin
    var I : Integer := 0;
    repeat
        I := I + 1;
    until I >= 20;
end;
```

**See Also**

[Looping Statements](#looping-statements) \| [Break and Continue](#break-and-continue)

#### Foreach Statement

The foreach statement provides a convenient shorthand for performing cursor or list iteration operations.

Foreach statements in D4 have the following syntax:

```bnf
<for statement> ::=
    foreach row | ([var] <qualified identifier>)
        in <cursor definition>
        do <statement>
```

The foreach statement is expanded by the compiler based on the result type of the iteration target expression. If the iteration target is a cursor-valued expression, the foreach statement is expanded into a cursor definition and the statement to be performed is placed inside a while loop iterating over each row in the result set of the cursor. Otherwise, the foreach statement is expanded into a list-style iteration using a for loop, an invocation of the Count operator, and an indexer expression within each iteration.

```bnf
#if <cursor definition> is cursor valued ::=
    begin
        var LCursor := <expression>;
        try
            #if var is specified
            var <qualified identifier> : typeof(LCursor.Select());
            #end
            while LCursor.Next() do
            begin
                #if <qualified identifier> is specified
                <qualified identifier> := LCursor.Select();
                #else
                #stackpush LCursor.Select()
                #end
                <statement>;
                #if <qualified identifier> is not specified
                #stackpop
                #end
            end;
        finally
            LCursor.Close();
        end;
    end;
#else
    begin
        #if var is specified
        var <qualified identifier> : typeof(<expression>[0]);
        #end
        for var LIndex := 1 to <expression>.Count() do
        begin
            <qualified identifier> := <expression>[LIndex];
            <statement>;
        end;
    end;
#end
```

If the **row** keyword is used, the columns of the row will be available within \<statement\> as variables.

The following examples illustrate the use of the foreach statement:

```d4
begin
    var LTable := table of { X : Integer, Y : Integer } { };
    for var LIndex := 1 to 100 do
        insert table { row { LIndex X, 101 - LIndex Y } } into LTable;

    var LSum := 0;
    foreach row in LTable do
        LSum := LSum + X;

    LSum := 0;
    foreach var LRow in LTable do
        LSum := LSum + LRow.X;

    LSum := 0;
    var LRow : typeof(LTable[]);
    foreach LRow in LTable do
        LSum := LSum + LRow.X;
end;

begin
    var LList := { "A", "B", "C", "D", "E" };

    var LString := "";
    foreach var LItem in LList do
        LString := LString + LItem;

    LString := "";
    var LItem : String;
    foreach LItem in LList do
        LString := LString + LItem;
end;
```

**See Also**

[Looping Statements](#looping-statements) \| [Break and Continue](#break-and-continue)

#### Break and Continue

Break and continue statements allow for fine grained control of loop iteration. The break statement breaks out of a given loop, passing control to the first statement after the loop. The continue statement jumps to the beginning of the next iteration, passing control to the beginning of the iterated statement.

Break and continue statements in D4 have the following syntax:

```bnf
<break statement> ::=
    break

<continue statement> ::=
    continue
```

It is an error to use a break or continue statement outside of a looping construct.

The following examples illustrate the use of the break and continue statements:

```d4
while true do
    break;
```

```d4
begin
    var EvenSum : Integer := 0;
    for I : Integer := 1 to 10 do
        if I mod 2 = 0 then
            EvenSum := EvenSum + I
        else
            continue;
    raise Error("EvenSum: " + EvenSum.ToString());
end;
```

**See Also**

[Looping Statements](#looping-statements)

## Exception Handling

Exception handling statements allow for the creation and handling of exceptions. An *exception* is used in D4 to indicate that an error has occurred. By relegating error handling to specialized statement blocks, the main body of a given operation can be developed optimistically. Only if something goes wrong is the error handling code invoked.

There are three types of error handling constructs in D4: the exception trap, the resource protection block, and the commit block. The [exception trap](#try-except-statement) allows exceptions of a specific type to be handled, the [resource protection block](#try-finally-statement) ensures that a specified statement is always executed, even if an exception occurs, and the [commit block](#try-commit-statement) is a shorthand for protecting a block of statements within a transaction.

### Raise Statement

The raise statement is used to throw a new exception. Control immediately passes from the raise statement to the first exception handler. If no exception handler is encountered, execution stops and the error message is reported to the client application. Note that if an implicit transaction was started by the CLI, the exception will cause the transaction to be rolled back, including any nested transactions that were started within the implicit transaction.

Raise statements in D4 have the following syntax:

```bnf
<raise statement> ::=
    raise [<expression>]
```

The [error value](../reference/system-library/types/System.Error.md) given by [\<expression](d4-language-elements.md#expressions)\> must be error-valued. The expression is optional only if the raise is invoked within an exception handler, in which case the error being handled is re-raised.

The following example illustrates the use of the raise statement:

```d4
if Count(TableVars) = 0 then
    raise Error("No table variables declared.");
```

**See Also**

[Exception Handling](#exception-handling)

### Try Except Statement

The try except statement acts as an exception filter, allowing the developer to trap for specific errors and take appropriate action.

Try except statements in D4 have the following syntax:

```bnf
<try except statement> ::=
    try
        <terminated statement list>
    except
        <terminated statement list> | <exception handler list>
    end

<exception handler> ::=
    on [<exception variable> :] <type specifier> do
        <terminated statement>

<exception variable> ::=
    <qualified identifier>
```

When an exception is raised within the try block, control immediately passes to the except block of the statement. If the except block is simply a list of statements, they are executed. If the except block is a list of exception handlers, the type of the exception is compared against each exception handler. Control is passed to the first exception handler matching the type of the exception. If no exception handler is found, control passes to the next try-except block in the program. This continues until an exception handler is found, or there are no containing try-except blocks, resulting in program termination.

Once an exception handler has been found, the exception is considered handled, and control passes to the first statement following the try-except block. Within an except block, the raise statement may be used without an argument to indicate that the exception should be re-raised from the exception block.

The following example illustrates the use of the try-except statement:

```d4
begin
        BeginTransaction();
        try
            raise Error("An error has occurred");
            CommitTransaction();
        except
        on LException : Error do
        begin
            RollbackTransaction();
            raise;
        end;
    end;
end;
```

**See Also**

[Exception Handling](#exception-handling)

### Try Finally Statement

The try finally statement in D4 allows a specified block of statements to be executed regardless of whether an exception occurs.

Try finally statements in D4 have the following syntax:

```bnf
<try finally statement> ::=
    try
        <terminated statement list>
    finally
        <terminated statement list>
    end
```

If no exception occurs within the try block, the finally block is executed normally. If an exception occurs within the try block, control passes immediately to the first statement in the finally block. All statements in the finally block are executed, and then exception processing continues as described in the try-except statement.

The following example illustrates the use of the try-finally statement:

```d4
begin
    var LCursor : cursor(table { Name : Name });
    LCursor := cursor(Objects over { Name });
    try
        // ... if an error occurs within this block
        // the cursor will still be closed
    finally
        LCursor.Close();
    end;
end;
```

**See Also**

[Exception Handling](#exception-handling)

### Try Commit Statement

The try commit statement in D4 protects a specified block of statements within a transaction.

Try commit statements in D4 have the following syntax:

```bnf
<try commit statement> ::=
    try
        <terminated statement lisst>
    commit
```

The try commit statement is shorthand for an equivalent try except statement with transaction management calls. For example, the following statement:

```d4
try
    insert table { row { 6 ID, 'Jacob' Name } } into Employee;
commit;
```

Is equivalent to the following try except statement:

```d4
begin
    BeginTransaction();
    try
        insert table { row { 6 ID, 'Jacob' Name } } into Employee;
        CommitTransaction();
    except
        RollbackTransaction();
        raise;
    end;
end;
```

**See Also**

[Exception Handling](#exception-handling)
