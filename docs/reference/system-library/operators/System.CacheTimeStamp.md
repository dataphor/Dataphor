# System.CacheTimeStamp

**Declarations**

```d4
create operator .System.CacheTimeStamp() : System.Long
 class "System.SystemCacheTimeStampNode"
    static tags
    { 
        
        DAE.IsDeterministic = "False", 
        DAE.IsLiteral = "False", 
        DAE.IsRemotable = "False"
    };
```

**Remarks**

`CacheTimeStamp` returns an integer value representing the current cache timestamp. The cache timestamp is incremented whenever the catalog is changed in a way that would affect a client-side cache. For example, if a table is added to the cache the cache timestamp is not changed. However, when a table listed in the cache is changed or removed the cache timestamp is updated.

**Using System.CacheTimeStamp**

```d4
select TableDee add { CacheTimeStamp() Cache };
```

**See Also**

[System.UpdateTimeStamps](System.UpdateTimeStamps.md)
