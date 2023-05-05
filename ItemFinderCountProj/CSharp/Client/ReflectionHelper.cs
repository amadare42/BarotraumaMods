
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System.Linq.Expressions;
using Barotrauma;
using Barotrauma.Items.Components;
using HarmonyLib;

namespace ItemFinderCount;

static class ReflectionHelper
{
    public static Func<MiniMap, Item, bool> VisibleOnItemFinder => _visibleOnItemFinder.Value;
    public static Lazy<Func<MiniMap, Item, bool>> _visibleOnItemFinder = new(CreateVisibleOnItemFinder);

    private static readonly Dictionary<int, object> GetPrivateFieldCache = new();
    
    public static TField GetPrivateField<TInstance, TField>(this TInstance instance, string name)
    {
        var hash = typeof(TInstance).GetHashCode() ^ name.GetHashCode();
        if (GetPrivateFieldCache.TryGetValue(hash, out var fn))
        {
            return ((Func<TInstance, TField>)fn)(instance);
        }

        var newFn = CreateAccessPrivateField<TInstance, TField>(name);
        GetPrivateFieldCache[hash] = newFn;
        return newFn(instance);
    }

    public static Func<MiniMap, Item, bool> CreateVisibleOnItemFinder()
    {
        var instanceParam = Expression.Parameter(typeof(MiniMap));
        var itemParam = Expression.Parameter(typeof(Item));
        var callExpr = Expression.Call(instanceParam, "VisibleOnItemFinder", Type.EmptyTypes, itemParam);
        var lambdaExpr = Expression.Lambda<Func<MiniMap, Item, bool>>(callExpr, instanceParam, itemParam);
        return lambdaExpr.Compile();
    }

    public static Func<TInstance, TField> CreateAccessPrivateField<TInstance, TField>(string name)
    {
        var instanceParam = Expression.Parameter(typeof(TInstance));
        var fieldExpr = Expression.Field(instanceParam, name);
        var lambdaExpr = Expression.Lambda<Func<TInstance, TField>>(fieldExpr, instanceParam);
        return lambdaExpr.Compile();
    }
}