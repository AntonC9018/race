The plugin to generate getters for all public things in a field, akin to `using` in jai.

```csharp
public struct A
{
    int b;
    string c;
}

[Properties]
public partial struct B
{
    [RefGetters]
    public A a;

    // generates:
    // ref int B => ref a.b;
    // ref int C => ref a.c;
}
```
