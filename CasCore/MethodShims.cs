using DouglasDwyer.CasCore;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Remoting;
using System.Security;
using System.Runtime.Loader;

namespace CasCore;

public static class MethodShims
{
    internal static IImmutableDictionary<MethodBase, MethodBase> ShimMap { get; } =
        typeof(MethodShims).GetMethods(BindingFlags.Public | BindingFlags.Static).Cast<MethodBase>().ToImmutableDictionary(GetOriginal, x => x);

    internal static IImmutableSet<RuntimeMethodHandle> ShimHandles { get; } = ShimMap.Select(x => x.Key.MethodHandle).ToImmutableHashSet();

    private const BindingFlags ConstructorDefault = BindingFlags.Instance | BindingFlags.Public | BindingFlags.CreateInstance;

    [DebuggerHidden]
    [DebuggerStepThrough]
    [StaticShim(typeof(Activator))]
    public static object? CreateInstance(Type type, BindingFlags bindingAttr, Binder? binder, object?[]? args, CultureInfo? culture) =>
        CreateInstance(Assembly.GetCallingAssembly(), type, bindingAttr, binder, args, culture, null);

    [DebuggerHidden]
    [DebuggerStepThrough]
    [StaticShim(typeof(Activator))]
    public static object? CreateInstance(Type type, params object?[]? args) =>
        CreateInstance(Assembly.GetCallingAssembly(), type, ConstructorDefault, null, args, null, null);

    [DebuggerHidden]
    [DebuggerStepThrough]
    [StaticShim(typeof(Activator))]
    public static object? CreateInstance(Type type, object?[]? args, object?[]? activationAttributes) =>
        CreateInstance(Assembly.GetCallingAssembly(), type, ConstructorDefault, null, args, null, activationAttributes);

    [DebuggerHidden]
    [DebuggerStepThrough]
    [StaticShim(typeof(Activator))]
    public static object? CreateInstance(Type type) =>
        CreateInstance(Assembly.GetCallingAssembly(), type, nonPublic: false);

    [StaticShim(typeof(Activator))]
    public static object? CreateInstance(Type type, BindingFlags bindingAttr, Binder? binder, object?[]? args, CultureInfo? culture, object?[]? activationAttributes)
        => CreateInstance(Assembly.GetCallingAssembly(), type, bindingAttr, binder, args, culture, null);

    [StaticShim(typeof(Activator))]
    public static object? CreateInstance(Type type, bool nonPublic)
        => CreateInstance(Assembly.GetCallingAssembly(), type, nonPublic);

    [StaticShim(typeof(Activator))]
    public static T CreateInstance<T>()
    {
        var constructor = typeof(T).GetConstructor([]);

        if (constructor is null)
        {
            throw new MissingMethodException($"Cannot find default constructor for {typeof(T)}.");
        }

        CasAssemblyLoader.AssertCanCall(Assembly.GetCallingAssembly(), null, constructor);
        return Activator.CreateInstance<T>();
    }

    private static object? CreateInstance(Assembly assembly, Type type, BindingFlags bindingAttr, Binder? binder, object?[]? args, CultureInfo? culture, object?[]? activationAttributes)
    {
        ArgumentNullException.ThrowIfNull(type);
        var constructors = type.GetConstructors(bindingAttr)
            .Where(x => ArgumentsBindable(x, args));

        foreach (var constructor in constructors)
        {
            CasAssemblyLoader.AssertCanCall(assembly, null, constructor);
        }

        return Activator.CreateInstance(type, bindingAttr, binder, args, culture, null);
    }

    private static object? CreateInstance(Assembly assembly, Type type, bool nonPublic)
    {
        ArgumentNullException.ThrowIfNull(type);
        var constructor = type.GetConstructor([]);

        if (constructor is null)
        {
            throw new MissingMethodException($"Cannot find default constructor for {type}.");
        }

        CasAssemblyLoader.AssertCanCall(assembly, null, constructor);
        return Activator.CreateInstance(type, nonPublic);
    }

    private static bool ArgumentsBindable(MethodBase method, object?[]? args)
    {
        var parameters = method.GetParameters();
        if (args is null)
        {
            return parameters.Length == 0;
        }
        else if (parameters.Length != args.Length)
        {
            return false;
        }
        else
        {
            for (var i = 0; i < parameters.Length; i++)
            {
                var arg = args[i];
                var param = parameters[i].ParameterType;
                if (arg is null)
                {
                    if (param.IsValueType)
                    {
                        return false;
                    }
                }
                else
                {
                    var underlyingType = param.IsByRef ? param.GetElementType()! : param;

                    if (!arg.GetType().IsAssignableTo(underlyingType))
                    {
                        return false;
                    }
                }
            }

            return true;
        }
    }

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