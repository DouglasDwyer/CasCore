using DouglasDwyer.CasCore;
using System.Collections.Immutable;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security;

namespace CasCore;

public static class MethodShims
{
    internal static IImmutableDictionary<MethodBase, MethodBase> ShimMap { get; } =
        typeof(MethodShims).GetMethods(BindingFlags.Public | BindingFlags.Static).Cast<MethodBase>().ToImmutableDictionary(GetOriginal, x => x);

    internal static IImmutableSet<RuntimeMethodHandle> ShimHandles { get; } = ShimMap.Select(x => x.Key.MethodHandle).ToImmutableHashSet();

    [StaticShim(typeof(RuntimeHelpers))]
    public static void InitializeArray(Array array, RuntimeFieldHandle fldHandle)
    {
        var field = FieldInfo.GetFieldFromHandle(fldHandle);
        CasAssemblyLoader.AssertCanAccess(Assembly.GetCallingAssembly(), field);
        RuntimeHelpers.InitializeArray(array, fldHandle);
    }

    public static object? GetValue(FieldInfo target, object? obj)
    {
        CasAssemblyLoader.AssertCanAccess(Assembly.GetCallingAssembly(), target);
        return target.GetValue(obj);
    }

    public static object? GetValue(PropertyInfo target, object? obj)
    {
        var getMethod = target.GetGetMethod();
        ArgumentNullException.ThrowIfNull(getMethod);
        CasAssemblyLoader.AssertCanCall(Assembly.GetCallingAssembly(), obj, getMethod);
        return target.GetValue(obj);
    }

    public static object? GetValue(PropertyInfo target, object? obj, object?[]? index)
    {
        var getMethod = target.GetGetMethod();
        ArgumentNullException.ThrowIfNull(getMethod);
        CasAssemblyLoader.AssertCanCall(Assembly.GetCallingAssembly(), obj, getMethod);
        return target.GetValue(obj, index);
    }

    public static object? GetValue(PropertyInfo target, object? obj, BindingFlags invokeAttr, Binder? binder, object?[]? index, CultureInfo? culture)
    {
        var getMethod = target.GetGetMethod();
        ArgumentNullException.ThrowIfNull(getMethod);
        CasAssemblyLoader.AssertCanCall(Assembly.GetCallingAssembly(), obj, getMethod);
        return target.GetValue(obj, invokeAttr, binder, index, culture);
    }

    public static void SetValue(FieldInfo target, object? obj, object? value)
    {
        CasAssemblyLoader.AssertCanAccess(Assembly.GetCallingAssembly(), target);
        target.SetValue(obj, value);
    }

    public static void SetValue(FieldInfo target, object? obj, object? value, BindingFlags invokeAttr, Binder? binder, CultureInfo? culture)
    {
        CasAssemblyLoader.AssertCanAccess(Assembly.GetCallingAssembly(), target);
        target.SetValue(obj, value, invokeAttr, binder, culture);
    }

    public static void SetValue(PropertyInfo target, object? obj, object? value)
    {
        var setMethod = target.GetSetMethod();
        ArgumentNullException.ThrowIfNull(setMethod);
        CasAssemblyLoader.AssertCanCall(Assembly.GetCallingAssembly(), obj, setMethod);
        target.SetValue(obj, value);
    }

    public static void SetValue(PropertyInfo target, object? obj, object? value, object?[]? index)
    {
        var setMethod = target.GetSetMethod();
        ArgumentNullException.ThrowIfNull(setMethod);
        CasAssemblyLoader.AssertCanCall(Assembly.GetCallingAssembly(), obj, setMethod);
        target.SetValue(obj, value, index);
    }

    public static void SetValue(PropertyInfo target, object? obj, object? value, BindingFlags invokeAttr, Binder? binder, object?[]? index, CultureInfo? culture)
    {
        var setMethod = target.GetGetMethod();
        ArgumentNullException.ThrowIfNull(setMethod);
        CasAssemblyLoader.AssertCanCall(Assembly.GetCallingAssembly(), obj, setMethod);
        target.SetValue(obj, value, invokeAttr, binder, index, culture);
    }

    public static object? Invoke(MethodBase target, object? obj, object?[]? parameters)
    {
        CasAssemblyLoader.AssertCanCall(Assembly.GetCallingAssembly(), obj, target);
        if (ShimHandles.Contains(target.MethodHandle))
        {
            throw new SecurityException($"Member {target} may not be invoked via reflection from an untrusted assembly.");
        }

        return target.Invoke(obj, parameters);
    }

    public static object? Invoke(MethodBase target, object? obj, BindingFlags invokeAttr, Binder? binder, object?[]? parameters, CultureInfo? culture)
    {
        CasAssemblyLoader.AssertCanCall(Assembly.GetCallingAssembly(), obj, target);
        if (ShimHandles.Contains(target.MethodHandle))
        {
            throw new SecurityException($"Member {target} may not be invoked via reflection from an untrusted assembly.");
        }

        return target.Invoke(obj, invokeAttr, binder, parameters, culture);
    }

    private static MethodBase GetOriginal(MethodBase method)
    {
        var bindingFlags = BindingFlags.Public | BindingFlags.NonPublic;
        var parameters = method.GetParameters();
        Type targetType;
        Type[] bindingParams;

        var staticShimAttr = method.GetCustomAttribute<StaticShimAttribute>();
        if (staticShimAttr is null)
        {
            targetType = parameters[0].ParameterType;
            bindingParams = parameters.Skip(1).Select(x => x.ParameterType).ToArray();
            bindingFlags |= BindingFlags.Instance;
        }
        else
        {
            targetType = staticShimAttr.Target;
            bindingParams = parameters.Select(x => x.ParameterType).ToArray();
            bindingFlags |= BindingFlags.Static;
        }

        var result = targetType.GetMethod(method.Name, bindingFlags, bindingParams);

        if (result is null)
        {
            throw new InvalidOperationException($"Could not find original method for shim {method}");
        }

        return result;
    }
}