using ID = System.UInt16;
// using ID = System.UInt32;
// using ID = System.Byte;

using System.Diagnostics;
using System;
using System.Runtime.CompilerServices;

namespace Queries;

public struct DynamicArray<T> where T : unmanaged
{
    T[] _array;
    int _count;

    public Span<T> Slice(int start, int length)
    {
        Debug.Assert(length <= _count);
        return _array.AsSpan(start, length);
    }
    public Span<T> Slice() => _array.AsSpan(0, _count);
    public int Length => _count;
    public int Capacity => _array.Length;

    public ref T this[int index]
    {
        get
        {
            Debug.Assert(index < _count);
            return ref _array[index];
        }
    }

    public void PopN(int count)
    {
        Debug.Assert(count <= _count);
        _count -= count;
    }

    public unsafe void AddSlice(Span<T> slice)
    {
        EnsureCapacity(slice.Length + Length);
        slice.CopyTo(Slice(_count, slice.Length));
        _count += slice.Length;
    }

    public void EnsureCapacity(int capacity)
    {
        if (Capacity < capacity)
            Array.Resize(ref _array, Math.Max(capacity, _array.Length * 2));
    }

    public static DynamicArray<T> Empty => new DynamicArray<T>{ _array = Array.Empty<T>(), _count = 0 };
}

// This can be put in the query base class, especially if we don't allow deleting queries.
// But if we do, keeping it inline is probably for the best.
public struct QuerySlot
{
    public uint updatedEpoch;
    // 2 could mean something special here.
    // TODO: The parent info does not seem to be needed since we do not requery on event dispatch.
    // But just epoch is not good enough, it does not capture the different events.
    // So perhaps clearing some sort of flag on some of the queries is needed on event dispatch.
    // We could only capture the data from the different event types if we requery everything on the clearing dispatch,
    // which might be wanted, depends on the problem.
    // If there are queries, you'd probably want them to calculate their thing each time, but it really depends.
    // Lazy evaluation is only possible if the queries depend only on the new state.
    // Could do a hybrid approach, allowing both.
    public ID parentCountOrFirstID;
    public ID startingIndexOrSecondID;
    public object query;
}

public interface ICache<TNewState>
{
    void Cache(TNewState state, QueryManager manager);
}

public interface IRefCache<TNewState> where TNewState : struct
{
    void Cache(in TNewState state, QueryManager manager);
}

public interface IValue<TValue>
{
    TValue Value { get; }
}

public interface IRefValue<TRefValue> where TRefValue : struct
{
    ref TRefValue Value { get; }
}

public class QueryManager
{
    private QuerySlot[] _slots;
    private DynamicArray<ID> _parentIDs;
    private uint _epoch;

    public QueryManager(int queryCount)
    {
        _slots = new QuerySlot[queryCount];
        _parentIDs = DynamicArray<ID>.Empty;
    }

    private ID _LastBitSet = (ID)((ID) 1) << ((sizeof(ID) * 8) - 1);
    
    public bool Has(ID id)
    {
        return _slots[id].query is not null;
    }

    public void Add(ID id, object query, Span<ID> parents)
    {
        Debug.Assert(!Has(id));
        ref var slot = ref _slots[id];
        if (parents.Length > 2)
        {
            Debug.Assert((((ID) parents.Length) & _LastBitSet) == 0);
            slot.parentCountOrFirstID = (ID) parents.Length;
            slot.startingIndexOrSecondID = (ID) _parentIDs.Length;
            _parentIDs.AddSlice(parents);
        }
        else if (parents.Length == 2)
        {
            slot.parentCountOrFirstID = (ID)(((ID) parents[0]) | _LastBitSet);
            slot.startingIndexOrSecondID = (ID) parents[1];
        }
        else if (parents.Length == 1)
        {
            slot.parentCountOrFirstID = 1;
            slot.startingIndexOrSecondID = (ID) parents[0];
        }
        else
        {
            slot.parentCountOrFirstID = 0;
            slot.startingIndexOrSecondID = 0;
        }
        slot.query = query;
        slot.updatedEpoch = 0;
    }

    public void BeginEpoch()
    {
        _epoch++;
    }

    public void MaybeCache<TNewState>(TNewState state, ID id) where TNewState : class
    {
        Debug.Assert(Has(id));
        ref var slot = ref _slots[id];
        if (_epoch > slot.updatedEpoch)
        {
            if ((slot.parentCountOrFirstID & _LastBitSet) != 0)
            {
                MaybeCache(state, (ID)(slot.parentCountOrFirstID ^ _LastBitSet));
                MaybeCache(state, slot.startingIndexOrSecondID);
            }
            else if (slot.parentCountOrFirstID == 1)
            {
                MaybeCache(state, slot.startingIndexOrSecondID);
            }
            else if (slot.parentCountOrFirstID != 0)
            {
                foreach (ID parentId in _parentIDs.Slice((int) slot.startingIndexOrSecondID, (int) slot.parentCountOrFirstID))
                    MaybeCache(state, parentId);
            }
            var q = (ICache<TNewState>) slot.query;
            q.Cache(state, this);
            slot.updatedEpoch = _epoch;
        }
    }

    public void MaybeRefCache<TNewState>(in TNewState state, ID id) where TNewState : struct
    {
        Debug.Assert(Has(id));
        ref var slot = ref _slots[id];
        if (_epoch > slot.updatedEpoch)
        {
            if ((slot.parentCountOrFirstID & _LastBitSet) != 0)
            {
                MaybeRefCache(in state, (ID)(slot.parentCountOrFirstID ^ _LastBitSet));
                MaybeRefCache(in state, slot.startingIndexOrSecondID);
            }
            else if (slot.parentCountOrFirstID == 1)
            {
                MaybeRefCache(in state, slot.startingIndexOrSecondID);
            }
            else if (slot.parentCountOrFirstID != 0)
            {
                foreach (ID parentId in _parentIDs.Slice((int) slot.startingIndexOrSecondID, (int) slot.parentCountOrFirstID))
                    MaybeRefCache(in state, parentId);
            }
            var q = (IRefCache<TNewState>) slot.query;
            q.Cache(in state, this);
            slot.updatedEpoch = _epoch;
        }
    }

    public TValue GetValue<TValue>(ID id)
    {
        Debug.Assert(Has(id));
        ref var slot = ref _slots[id];
        Debug.Assert(slot.updatedEpoch == _epoch);
        return ((IValue<TValue>) slot.query).Value;
    }

    public ref TRefValue GetRefValue<TRefValue>(ID id) where TRefValue : struct
    {
        Debug.Assert(Has(id));
        ref var slot = ref _slots[id];
        Debug.Assert(slot.updatedEpoch == _epoch);
        return ref ((IRefValue<TRefValue>) slot.query).Value;
    }
}


public struct State
{
    public int val0;
    public float val1;
    public string val2;
}

public class A : IRefCache<State>, IValue<float>
{
    public static readonly ID ID = 0;
    public float Value { get; set; }
    public void Cache(in State state, QueryManager manager)
    {
        Value = state.val1 + 1;
    }
}

public class B : IRefCache<State>, IValue<float>
{
    public static readonly ID ID = 1;
    public float Value { get; set; }
    public void Cache(in State state, QueryManager manager)
    {
        Value = manager.GetValue<float>(A.ID) + 1;
    }
}

public static class Extensions
{
    public static void Add(this QueryManager queryManager, A a)
    {
        Span<ID> parentIDs = stackalloc ID[0];
        queryManager.Add(A.ID, a, parentIDs);
    }
    public static void Add(this QueryManager queryManager, B b)
    {
        Span<ID> parentIDs = stackalloc ID[1];
        parentIDs[0] = A.ID;

        queryManager.Add(B.ID, b, parentIDs);

        if (!queryManager.Has(A.ID))
            queryManager.Add(new A());
    }
}

public class Program
{
    public static void Main(string[] args)
    {
        var manager = new QueryManager(2);
        manager.Add(new A());
        manager.Add(new B());
        manager.BeginEpoch();

        var state = new State
        {
            val1 = 10,
        };
        manager.MaybeRefCache(in state, B.ID);
        var valb = manager.GetValue<float>(B.ID);
        var vala = manager.GetValue<float>(A.ID);

        Console.WriteLine(valb);
        Console.WriteLine(vala);
    }
}