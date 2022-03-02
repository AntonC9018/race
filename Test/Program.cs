
using System.Runtime.CompilerServices;

public readonly unsafe ref struct StackRef<T>
{
    public readonly void* Address;

    public ref T Value => ref Unsafe.AsRef<T>(Address);

    public StackRef(ref T value)
    {
        Address = Unsafe.AsPointer(ref value);
    }
}

public class A
{
    public int i;
}

public readonly struct Info
{
    public readonly A b;
    public readonly A a;
    public Info(A a) { this.a = a; b = null; }
}

public class Program
{
    public static void Main(string[] args)
    {
        var a = new A();
        var info = new Info(a);
        var stackRef = new StackRef<Info>(ref info);
        stackRef.Value.a.i = 5;
        System.Console.WriteLine(a.i);
        
        // Need to call this at the end of the scope so that the managed fields
        // are referenced. Otherwise the GC might delete them mid function call
        // https://source.dot.net/#System.Private.CoreLib/src/System/GC.cs,238
        // Another way is to define an empty method with MethodImpl(MethodImplOptions.NoInlining)
        // and call that at the end of scope.
        //
        // Speaking of Unity events, if that's the only use case, I can just
        // generate wrappers for calling those, which would do the keepalive
        // business automatically
        GC.KeepAlive(info.a);
        GC.KeepAlive(info.b);
    }
}