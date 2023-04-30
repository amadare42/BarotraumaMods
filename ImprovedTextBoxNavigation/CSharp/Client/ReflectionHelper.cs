using System.Linq.Expressions;
using System.Reflection;
using Barotrauma;
using Barotrauma.Items.Components;
using HarmonyLib;

namespace ImprovedTextBoxNavigation;

static class ReflectionHelper
{
    private static readonly Dictionary<int, object> GetPrivateFieldCache = new();
    private static readonly Dictionary<int, object> SetPrivateFieldCache = new();
    private static readonly Dictionary<int, object> PrivateMethodCallCache = new();
    
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

    public static void SetPrivateField<TInstance, TField>(this TInstance instance, string name, TField value)
    {        
        var hash = typeof(TInstance).GetHashCode() ^ name.GetHashCode();
        if (SetPrivateFieldCache.TryGetValue(hash, out var fn))
        {
            ((Action<TInstance, TField>)fn)(instance, value);
        }

        var newFn = CreateSetPrivateField<TInstance, TField>(name);
        SetPrivateFieldCache[hash] = newFn;
        newFn(instance, value);
    }

    public static Func<TInstance, TField> CreateAccessPrivateField<TInstance, TField>(string name)
    {
        var instanceParam = Expression.Parameter(typeof(TInstance));
        var fieldExpr = Expression.Field(instanceParam, name);
        var lambdaExpr = Expression.Lambda<Func<TInstance, TField>>(fieldExpr, instanceParam);
        return lambdaExpr.Compile();
    }

    public static Action<TInstance, TField> CreateSetPrivateField<TInstance, TField>(string name)
	{
		var instanceParam = Expression.Parameter(typeof(TInstance));
		var valueParam = Expression.Parameter(typeof(TField));
		var fieldExpr = Expression.Field(instanceParam, name);
		var assignExpr = Expression.Assign(fieldExpr, valueParam);
		var lambdaExpr = Expression.Lambda<Action<TInstance, TField>>(assignExpr, instanceParam, valueParam);
		return lambdaExpr.Compile();
	}

	public static TDeleg CreateAccessPrivateMethod<TDeleg, TInstance>(string name, Type[] paramTypes)
	{
		var instanceParam = Expression.Parameter(typeof(TInstance));
		var otherArgs = paramTypes.Select(p => Expression.Parameter(p)).ToArray();
		var args = new ParameterExpression[] { instanceParam }.Concat(otherArgs).ToArray();
		var callExpr = Expression.Call(instanceParam, typeof(TInstance).GetMethod(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance, paramTypes), otherArgs);
		var lambdaExpr = Expression.Lambda<TDeleg>(callExpr, args);
		return lambdaExpr.Compile();
	}

	public static TDeleg CreateAccessPrivateMethod<TDeleg, TInstance>(string name)
	{
		var instanceParam = Expression.Parameter(typeof(TInstance));
		var callExpr = Expression.Call(instanceParam, typeof(TInstance).GetMethod(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance));
		var lambdaExpr = Expression.Lambda<TDeleg>(callExpr, instanceParam);
		return lambdaExpr.Compile();
	}
}