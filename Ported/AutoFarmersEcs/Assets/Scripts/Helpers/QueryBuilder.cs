using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Entities;

/// <summary>
/// Helper for building entity query using fluent syntax.
/// </summary>
/// <remarks>Reimplementation of <see cref="EntityQueryBuilder"/>.</remarks>
public struct QueryBuilder
{
    public readonly SystemBase System;

    private uint m_AnyWritableBitField;
    private uint m_AllWritableBitField;
    private FixedListInt64 m_Any;
    private FixedListInt64 m_None;
    private FixedListInt64 m_All;
    private EntityQueryOptions m_Options;

    public QueryBuilder(SystemBase system)
    {
        System = system;
        m_Any = default;
        m_None = default;
        m_All = default;
        m_AnyWritableBitField = (m_AllWritableBitField = 0u);
        m_Options = EntityQueryOptions.Default;
    }

    public EntityQueryDesc ToEntityQueryDesc()
    {
        return ToEntityQueryDesc(0);
    }

    private EntityQueryDesc ToEntityQueryDesc(int delegateTypeCount)
    {
        return new EntityQueryDesc
        {
            Any = ToComponentTypes(ref m_Any, m_AnyWritableBitField, 0),
            None = ToComponentTypes(ref m_None, 0u, 0),
            All = ToComponentTypes(ref m_All, m_AllWritableBitField, delegateTypeCount),
            Options = m_Options
        };
    }

    static ComponentType[] ToComponentTypes(ref FixedListInt64 typeIndices, uint writableBitField, int extraCapacity)
    {
        int length = typeIndices.Length + extraCapacity;
        if (length == 0)
        {
            return Array.Empty<ComponentType>();
        }
        ComponentType[] types = new ComponentType[length];
        for (int i = 0; i < typeIndices.Length; i++)
        {
            types[i] = new ComponentType
            {
                TypeIndex = typeIndices[i],
                AccessModeType = (((writableBitField & (1 << i)) == 0L) ? ComponentType.AccessMode.ReadOnly : ComponentType.AccessMode.ReadWrite)
            };
        }
        return types;
    }

    public QueryBuilder WithAny<T0>()
    {
        m_Any.Add(TypeManager.GetTypeIndex<T0>());
        m_AnyWritableBitField |= (uint)(((1 << m_Any.Length) - 1) ^ ((1 << m_Any.Length - 1) - 1));
        return this;
    }

    public QueryBuilder WithAny(ComponentType type0)
    {
        m_Any.Add(type0.TypeIndex);
        m_AnyWritableBitField |= (uint)(((1 << m_Any.Length) - 1) ^ ((1 << m_Any.Length - 1) - 1));
        return this;
    }

    public QueryBuilder WithAny<T0, T1>()
    {
        m_Any.Add(TypeManager.GetTypeIndex<T0>());
        m_Any.Add(TypeManager.GetTypeIndex<T1>());
        m_AnyWritableBitField |= (uint)(((1 << m_Any.Length) - 1) ^ ((1 << m_Any.Length - 2) - 1));
        return this;
    }

    public QueryBuilder WithAny(ComponentType type0, ComponentType type1)
    {
        m_Any.Add(type0.TypeIndex);
        m_Any.Add(type1.TypeIndex);
        m_AnyWritableBitField |= (uint)(((1 << m_Any.Length) - 1) ^ ((1 << m_Any.Length - 2) - 1));
        return this;
    }

    public QueryBuilder WithAny<T0, T1, T2>()
    {
        m_Any.Add(TypeManager.GetTypeIndex<T0>());
        m_Any.Add(TypeManager.GetTypeIndex<T1>());
        m_Any.Add(TypeManager.GetTypeIndex<T2>());
        m_AnyWritableBitField |= (uint)(((1 << m_Any.Length) - 1) ^ ((1 << m_Any.Length - 3) - 1));
        return this;
    }

    public QueryBuilder WithAny(ComponentType type0, ComponentType type1, ComponentType type2)
    {
        m_Any.Add(type0.TypeIndex);
        m_Any.Add(type1.TypeIndex);
        m_Any.Add(type2.TypeIndex);
        m_AnyWritableBitField |= (uint)(((1 << m_Any.Length) - 1) ^ ((1 << m_Any.Length - 3) - 1));
        return this;
    }

    public QueryBuilder WithAny<T0, T1, T2, T3>()
    {
        m_Any.Add(TypeManager.GetTypeIndex<T0>());
        m_Any.Add(TypeManager.GetTypeIndex<T1>());
        m_Any.Add(TypeManager.GetTypeIndex<T2>());
        m_Any.Add(TypeManager.GetTypeIndex<T3>());
        m_AnyWritableBitField |= (uint)(((1 << m_Any.Length) - 1) ^ ((1 << m_Any.Length - 4) - 1));
        return this;
    }

    public QueryBuilder WithAny(ComponentType type0, ComponentType type1, ComponentType type2, ComponentType type3)
    {
        m_Any.Add(type0.TypeIndex);
        m_Any.Add(type1.TypeIndex);
        m_Any.Add(type2.TypeIndex);
        m_Any.Add(type3.TypeIndex);
        m_AnyWritableBitField |= (uint)(((1 << m_Any.Length) - 1) ^ ((1 << m_Any.Length - 4) - 1));
        return this;
    }

    public QueryBuilder WithAny<T0, T1, T2, T3, T4>()
    {
        m_Any.Add(TypeManager.GetTypeIndex<T0>());
        m_Any.Add(TypeManager.GetTypeIndex<T1>());
        m_Any.Add(TypeManager.GetTypeIndex<T2>());
        m_Any.Add(TypeManager.GetTypeIndex<T3>());
        m_Any.Add(TypeManager.GetTypeIndex<T4>());
        m_AnyWritableBitField |= (uint)(((1 << m_Any.Length) - 1) ^ ((1 << m_Any.Length - 5) - 1));
        return this;
    }

    public QueryBuilder WithAny(ComponentType type0, ComponentType type1, ComponentType type2, ComponentType type3, ComponentType type4)
    {
        m_Any.Add(type0.TypeIndex);
        m_Any.Add(type1.TypeIndex);
        m_Any.Add(type2.TypeIndex);
        m_Any.Add(type3.TypeIndex);
        m_Any.Add(type4.TypeIndex);
        m_AnyWritableBitField |= (uint)(((1 << m_Any.Length) - 1) ^ ((1 << m_Any.Length - 5) - 1));
        return this;
    }

    public QueryBuilder WithAnyReadOnly<T0>()
    {
        m_Any.Add(TypeManager.GetTypeIndex<T0>());
        return this;
    }

    public QueryBuilder WithAnyReadOnly(ComponentType type0)
    {
        m_Any.Add(type0.TypeIndex);
        return this;
    }

    public QueryBuilder WithAnyReadOnly<T0, T1>()
    {
        m_Any.Add(TypeManager.GetTypeIndex<T0>());
        m_Any.Add(TypeManager.GetTypeIndex<T1>());
        return this;
    }

    public QueryBuilder WithAnyReadOnly(ComponentType type0, ComponentType type1)
    {
        m_Any.Add(type0.TypeIndex);
        m_Any.Add(type1.TypeIndex);
        return this;
    }

    public QueryBuilder WithAnyReadOnly<T0, T1, T2>()
    {
        m_Any.Add(TypeManager.GetTypeIndex<T0>());
        m_Any.Add(TypeManager.GetTypeIndex<T1>());
        m_Any.Add(TypeManager.GetTypeIndex<T2>());
        return this;
    }

    public QueryBuilder WithAnyReadOnly(ComponentType type0, ComponentType type1, ComponentType type2)
    {
        m_Any.Add(type0.TypeIndex);
        m_Any.Add(type1.TypeIndex);
        m_Any.Add(type2.TypeIndex);
        return this;
    }

    public QueryBuilder WithAnyReadOnly<T0, T1, T2, T3>()
    {
        
        m_Any.Add(TypeManager.GetTypeIndex<T0>());
        m_Any.Add(TypeManager.GetTypeIndex<T1>());
        m_Any.Add(TypeManager.GetTypeIndex<T2>());
        m_Any.Add(TypeManager.GetTypeIndex<T3>());
        return this;
    }

    public QueryBuilder WithAnyReadOnly(ComponentType type0, ComponentType type1, ComponentType type2, ComponentType type3)
    {
        
        m_Any.Add(type0.TypeIndex);
        m_Any.Add(type1.TypeIndex);
        m_Any.Add(type2.TypeIndex);
        m_Any.Add(type3.TypeIndex);
        return this;
    }

    public QueryBuilder WithAnyReadOnly<T0, T1, T2, T3, T4>()
    {
        
        m_Any.Add(TypeManager.GetTypeIndex<T0>());
        m_Any.Add(TypeManager.GetTypeIndex<T1>());
        m_Any.Add(TypeManager.GetTypeIndex<T2>());
        m_Any.Add(TypeManager.GetTypeIndex<T3>());
        m_Any.Add(TypeManager.GetTypeIndex<T4>());
        return this;
    }

    public QueryBuilder WithAnyReadOnly(ComponentType type0, ComponentType type1, ComponentType type2, ComponentType type3, ComponentType type4)
    {
        
        m_Any.Add(type0.TypeIndex);
        m_Any.Add(type1.TypeIndex);
        m_Any.Add(type2.TypeIndex);
        m_Any.Add(type3.TypeIndex);
        m_Any.Add(type4.TypeIndex);
        return this;
    }

    public QueryBuilder WithNone<T0>()
    {
        
        m_None.Add(TypeManager.GetTypeIndex<T0>());
        return this;
    }

    public QueryBuilder WithNone(ComponentType type0)
    {
        
        m_None.Add(type0.TypeIndex);
        return this;
    }

    public QueryBuilder WithNone<T0, T1>()
    {
        
        m_None.Add(TypeManager.GetTypeIndex<T0>());
        m_None.Add(TypeManager.GetTypeIndex<T1>());
        return this;
    }

    public QueryBuilder WithNone(ComponentType type0, ComponentType type1)
    {
        
        m_None.Add(type0.TypeIndex);
        m_None.Add(type1.TypeIndex);
        return this;
    }

    public QueryBuilder WithNone<T0, T1, T2>()
    {
        
        m_None.Add(TypeManager.GetTypeIndex<T0>());
        m_None.Add(TypeManager.GetTypeIndex<T1>());
        m_None.Add(TypeManager.GetTypeIndex<T2>());
        return this;
    }

    public QueryBuilder WithNone(ComponentType type0, ComponentType type1, ComponentType type2)
    {
        
        m_None.Add(type0.TypeIndex);
        m_None.Add(type1.TypeIndex);
        m_None.Add(type2.TypeIndex);
        return this;
    }

    public QueryBuilder WithNone<T0, T1, T2, T3>()
    {
        
        m_None.Add(TypeManager.GetTypeIndex<T0>());
        m_None.Add(TypeManager.GetTypeIndex<T1>());
        m_None.Add(TypeManager.GetTypeIndex<T2>());
        m_None.Add(TypeManager.GetTypeIndex<T3>());
        return this;
    }

    public QueryBuilder WithNone(ComponentType type0, ComponentType type1, ComponentType type2, ComponentType type3)
    {
        
        m_None.Add(type0.TypeIndex);
        m_None.Add(type1.TypeIndex);
        m_None.Add(type2.TypeIndex);
        m_None.Add(type3.TypeIndex);
        return this;
    }

    public QueryBuilder WithNone<T0, T1, T2, T3, T4>()
    {
        
        m_None.Add(TypeManager.GetTypeIndex<T0>());
        m_None.Add(TypeManager.GetTypeIndex<T1>());
        m_None.Add(TypeManager.GetTypeIndex<T2>());
        m_None.Add(TypeManager.GetTypeIndex<T3>());
        m_None.Add(TypeManager.GetTypeIndex<T4>());
        return this;
    }

    public QueryBuilder WithNone(ComponentType type0, ComponentType type1, ComponentType type2, ComponentType type3, ComponentType type4)
    {
        
        m_None.Add(type0.TypeIndex);
        m_None.Add(type1.TypeIndex);
        m_None.Add(type2.TypeIndex);
        m_None.Add(type3.TypeIndex);
        m_None.Add(type4.TypeIndex);
        return this;
    }

    public QueryBuilder WithAll<T0>()
    {
        
        m_All.Add(TypeManager.GetTypeIndex<T0>());
        m_AllWritableBitField |= (uint)(((1 << m_All.Length) - 1) ^ ((1 << m_All.Length - 1) - 1));
        return this;
    }

    public QueryBuilder WithAll(ComponentType type0)
    {
        
        m_All.Add(type0.TypeIndex);
        m_AllWritableBitField |= (uint)(((1 << m_All.Length) - 1) ^ ((1 << m_All.Length - 1) - 1));
        return this;
    }

    public QueryBuilder WithAll<T0, T1>()
    {
        
        m_All.Add(TypeManager.GetTypeIndex<T0>());
        m_All.Add(TypeManager.GetTypeIndex<T1>());
        m_AllWritableBitField |= (uint)(((1 << m_All.Length) - 1) ^ ((1 << m_All.Length - 2) - 1));
        return this;
    }

    public QueryBuilder WithAll(ComponentType type0, ComponentType type1)
    {
        
        m_All.Add(type0.TypeIndex);
        m_All.Add(type1.TypeIndex);
        m_AllWritableBitField |= (uint)(((1 << m_All.Length) - 1) ^ ((1 << m_All.Length - 2) - 1));
        return this;
    }

    public QueryBuilder WithAll<T0, T1, T2>()
    {
        
        m_All.Add(TypeManager.GetTypeIndex<T0>());
        m_All.Add(TypeManager.GetTypeIndex<T1>());
        m_All.Add(TypeManager.GetTypeIndex<T2>());
        m_AllWritableBitField |= (uint)(((1 << m_All.Length) - 1) ^ ((1 << m_All.Length - 3) - 1));
        return this;
    }

    public QueryBuilder WithAll(ComponentType type0, ComponentType type1, ComponentType type2)
    {
        
        m_All.Add(type0.TypeIndex);
        m_All.Add(type1.TypeIndex);
        m_All.Add(type2.TypeIndex);
        m_AllWritableBitField |= (uint)(((1 << m_All.Length) - 1) ^ ((1 << m_All.Length - 3) - 1));
        return this;
    }

    public QueryBuilder WithAll<T0, T1, T2, T3>()
    {
        
        m_All.Add(TypeManager.GetTypeIndex<T0>());
        m_All.Add(TypeManager.GetTypeIndex<T1>());
        m_All.Add(TypeManager.GetTypeIndex<T2>());
        m_All.Add(TypeManager.GetTypeIndex<T3>());
        m_AllWritableBitField |= (uint)(((1 << m_All.Length) - 1) ^ ((1 << m_All.Length - 4) - 1));
        return this;
    }

    public QueryBuilder WithAll(ComponentType type0, ComponentType type1, ComponentType type2, ComponentType type3)
    {
        
        m_All.Add(type0.TypeIndex);
        m_All.Add(type1.TypeIndex);
        m_All.Add(type2.TypeIndex);
        m_All.Add(type3.TypeIndex);
        m_AllWritableBitField |= (uint)(((1 << m_All.Length) - 1) ^ ((1 << m_All.Length - 4) - 1));
        return this;
    }

    public QueryBuilder WithAll<T0, T1, T2, T3, T4>()
    {
        
        m_All.Add(TypeManager.GetTypeIndex<T0>());
        m_All.Add(TypeManager.GetTypeIndex<T1>());
        m_All.Add(TypeManager.GetTypeIndex<T2>());
        m_All.Add(TypeManager.GetTypeIndex<T3>());
        m_All.Add(TypeManager.GetTypeIndex<T4>());
        m_AllWritableBitField |= (uint)(((1 << m_All.Length) - 1) ^ ((1 << m_All.Length - 5) - 1));
        return this;
    }

    public QueryBuilder WithAll(ComponentType type0, ComponentType type1, ComponentType type2, ComponentType type3, ComponentType type4)
    {
        
        m_All.Add(type0.TypeIndex);
        m_All.Add(type1.TypeIndex);
        m_All.Add(type2.TypeIndex);
        m_All.Add(type3.TypeIndex);
        m_All.Add(type4.TypeIndex);
        m_AllWritableBitField |= (uint)(((1 << m_All.Length) - 1) ^ ((1 << m_All.Length - 5) - 1));
        return this;
    }

    public static implicit operator EntityQuery(QueryBuilder builder)
    {
        return builder.System.GetEntityQuery(builder.ToEntityQueryDesc());
    }
}
