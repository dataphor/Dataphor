# System.UpdateTimeStamps

**Declarations**

```d4
create operator .System.UpdateTimeStamps()
 class "System.SystemUpdateTimeStampsNode" static tags { DAE.IsRemotable = "False", DAE.IsFunctional = "False" };
```

**Remarks**

`UpdateTimeStamps` updates the various timestamps tracked for the system catalog. This is useful for forcing a refresh of the various caches based on these timestamps such as client-side catalog caches, and the Frontend Server's derivation cache.

**Using System.UpdateTimeStamps**

```d4
select TableDee add {CacheTimeStamp() Cache};
UpdateTimeStamps();
select TableDee add {CacheTimeStamp() Cache};
```

```text
Cache 
----- 
182

Cache 
----- 
183
```

**See Also**

[System.CacheTimeStamp](System.CacheTimeStamp.md)
