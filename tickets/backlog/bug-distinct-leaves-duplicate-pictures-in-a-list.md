description: Asking for the distinct values of a list of pictures or documents gives back the duplicates as well, because the engine sorts such values into buckets by where they are stored rather than by what they contain, so two equal values are never compared. The same is true for a list of rows or of lists.
architecture: docs/modernization-plan.md#ws2-engine-and-server-port
files: src/engine/DAE/Runtime.Data/NativeSet.cs, src/engine/DAE/Runtime.Instructions/ListNodes.cs, tests/libraries/TestFramework.Coverage.Scripts/Documents/StreamValues.d4
repro: verified
severity: wrong-result
likelihood: unusual
tradeoffs: Nothing shipped calls `Distinct` over a list of these types - the only use anywhere in the tree is in parked code - so a maintainer may well decide that an operator with no caller can wait, and the fix needs a decision about how to hash a value the engine does not keep inline, which is more than a point change.
----
## What happens

`Distinct(const AList : list)` - written `AList.Distinct()` in D4 - returns the list with its duplicates removed. It does that for a list of `String` or `Integer` and does not do it for a list of `Binary` or `Graphic`, or for a list of rows or of lists: the result is the source list, duplicates and all.

Measured on 2026-11-24, on an in-process server, with `bug-a-list-value-never-releases-its-elements` applied:

```
var LL : list(Binary) := { Binary('ABEiM0RVZnc='), Binary('qrvM3Q=='), Binary('ABEiM0RVZnc=') };
var LD : list(Binary) := LL.Distinct();
```

`LD.Count()` is 3, and its first and last elements both read back as the first value. A list of `String` with the same shape gives 2, which is the answer this one should give.

## Why

The operator builds a set of the elements it has already seen (`NativeSet`, `src/engine/DAE/Runtime.Data/NativeSet.cs`) and asks it whether each next element is in it. The set is a `HashSet<object>` with a comparer whose equality goes through the engine's equality operator for the element type - which is correct - and whose `GetHashCode` is `obj.GetHashCode()`, the hash .NET gives the *native* value. For a value the engine keeps inline (a `string`, a boxed `int`) that is a hash of the contents and everything works. For everything else it is a hash of where the value is: a `Binary` is a `StreamID`, which hashes as the number naming its stream, and a row, list or table is a native structure that hashes by reference. Two equal values therefore hash differently, land in different buckets, and the comparer's equality is never consulted.

So the defect is one line, but the fix is a decision rather than an edit: there is no hash operator in D4 to ask for the contents of a value, and `Schema.Sort` carries a comparison rather than a hash. Three shapes a fix could take, for whoever picks this up to weigh rather than as a recommendation:

- Give the set a linear fallback: keep the hash set for element types whose native values hash by value, and compare against every element held so far for the rest. Correct for every type, quadratic in the number of distinct elements, and `Distinct` is already building a whole second container.
- Derive a hash from the value's physical representation (`DataValue.GetPhysicalSize`/`WriteToPhysical`, which every type implements), so that equal values hash equally. Costs a serialization per element and needs care that two values the engine calls equal always serialize identically - which a type with a custom equality operator can break.
- Declare a hash in the type system, as a representation or an operator a scalar type can carry, and fall back to one of the above when a type does not. The largest change and the only one that puts the question where the answer lives.

## What is already recorded

A `NOTE:` at the comparer's `GetHashCode` states the measurement and names this ticket. The coverage script `StreamValues.d4` has a block for `Distinct` over a list of `Binary` - added by `bug-a-list-value-never-releases-its-elements`, which fixed a separate defect at the same call site (the set released the elements the source list still owned) - and that block deliberately asserts only that both distinct values are present and readable, not how many elements the result has, so it will keep passing when this is fixed. Pinning the count is part of the work here.

## Use cases

- A list of pictures or documents, deduplicated: `MyPictures.Distinct().Count()` must report the number of distinct pictures.
- A list of rows, deduplicated: two rows with equal column values must collapse to one.
- A list of `String` or `Integer`, which works today, must keep working - that is the regression risk in any fix.
