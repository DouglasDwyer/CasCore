using DouglasDwyer.CasCore;

using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security;

namespace CasCore;

public static class MethodShims
{
    private static IImmutableDictionary<MethodBase, MethodBase> ReplacementShims { get; } =
        typeof(MethodShims).GetMethods(BindingFlags.Public | BindingFlags.Static).Cast<MethodBase>().ToImmutableDictionary(GetOriginal, x => x);

    internal static IImmutableDictionary<SignatureHash, MethodBase> ShimMap { get; } =
        ReplacementShims.ToImmutableDictionary(x => new SignatureHash(x.Key), x => x.Value);

    internal static IImmutableSet<RuntimeMethodHandle> ShimHandles { get; } = ReplacementShims.Select(x => x.Key.MethodHandle).ToImmutableHashSet();

    private const BindingFlags ConstructorDefault = BindingFlags.Instance | BindingFlags.Public | BindingFlags.CreateInstance;

    /** System.Linq.Expressions.Expression<T> shims **/

    public static T Compile<T>(Expression<T> target)
        => CompileExpression(Assembly.GetCallingAssembly(), target);

    public static T Compile<T>(Expression<T> target, bool preferInterpretation)
        => CompileExpression(Assembly.GetCallingAssembly(), target);

    public static T Compile<T>(Expression<T> target, DebugInfoGenerator generator)
        => CompileExpression(Assembly.GetCallingAssembly(), target);

    private static T CompileExpression<T>(Assembly assembly, Expression<T> target)
    {
        return new GuardExpressionVisitor(assembly)
            .VisitAndConvert(target, "Compile")
            .Compile(true);
    }

    /** System.Linq.Expressions.LambdaExpression shims **/

    public static Delegate Compile(LambdaExpression target)
        => CompileLambda(Assembly.GetCallingAssembly(), target);

    public static Delegate Compile(LambdaExpression target, bool preferInterpretation)
        => CompileLambda(Assembly.GetCallingAssembly(), target);

    private static Delegate CompileLambda(Assembly assembly, LambdaExpression target)
    {
        return new GuardExpressionVisitor(assembly)
            .VisitAndConvert(target, "Compile")
            .Compile(true);
    }

    /** System.Delegate shims **/

    [StaticShim(typeof(Delegate))]
    public static Delegate? CreateDelegate(Type type, object target, string method, bool ignoreCase, bool throwOnBindFailure) => CheckAndReturnDelegate(Assembly.GetCallingAssembly(), Delegate.CreateDelegate(type, target, method, ignoreCase, throwOnBindFailure));

    [StaticShim(typeof(Delegate))]
    public static Delegate CreateDelegate(Type type, Type target, string method, bool ignoreCase) => CheckAndReturnDelegate(Assembly.GetCallingAssembly(), Delegate.CreateDelegate(type, target, method, ignoreCase));

    [StaticShim(typeof(Delegate))]
    public static Delegate CreateDelegate(Type type, object target, string method, bool ignoreCase) => CheckAndReturnDelegate(Assembly.GetCallingAssembly(), Delegate.CreateDelegate(type, target, method, ignoreCase));

    [StaticShim(typeof(Delegate))]
    public static Delegate? CreateDelegate(Type type, Type target, string method, bool ignoreCase, bool throwOnBindFailure) => CheckAndReturnDelegate(Assembly.GetCallingAssembly(), Delegate.CreateDelegate(type, target, method, ignoreCase, throwOnBindFailure));

    [StaticShim(typeof(Delegate))]
    public static Delegate? CreateDelegate(Type type, object? firstArgument, MethodInfo method, bool throwOnBindFailure) => CheckAndReturnDelegate(Assembly.GetCallingAssembly(), Delegate.CreateDelegate(type, firstArgument, method, throwOnBindFailure));

    [StaticShim(typeof(Delegate))]
    public static Delegate? CreateDelegate(Type type, MethodInfo method, bool throwOnBindFailure) => CheckAndReturnDelegate(Assembly.GetCallingAssembly(), Delegate.CreateDelegate(type, method, throwOnBindFailure));

    [StaticShim(typeof(Delegate))]
    public static Delegate CreateDelegate(Type type, object? firstArgument, MethodInfo method) => CheckAndReturnDelegate(Assembly.GetCallingAssembly(), Delegate.CreateDelegate(type, firstArgument, method));

    [StaticShim(typeof(Delegate))]
    public static Delegate CreateDelegate(Type type, MethodInfo method) => CheckAndReturnDelegate(Assembly.GetCallingAssembly(), Delegate.CreateDelegate(type, method));

    [StaticShim(typeof(Delegate))]
    public static Delegate CreateDelegate(Type type, object target, string method) => CheckAndReturnDelegate(Assembly.GetCallingAssembly(), Delegate.CreateDelegate(type, target, method));

    [StaticShim(typeof(Delegate))]
    public static Delegate CreateDelegate(Type type, Type target, string method) => CheckAndReturnDelegate(Assembly.GetCallingAssembly(), Delegate.CreateDelegate(type, target, method));

    [return: NotNullIfNotNull(nameof(uncheckedDelegate))]
    private static T? CheckAndReturnDelegate<T>(Assembly assembly, T? uncheckedDelegate) where T : Delegate
    {
        if (uncheckedDelegate is not null)
        {
            CasAssemblyLoader.AssertCanCall(assembly, uncheckedDelegate.Target, uncheckedDelegate.Method);
        }

        return uncheckedDelegate;
    }

    /** System.Activator shims **/

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
        if (!typeof(T).IsPrimitive)
        {
            var constructor = typeof(T).GetConstructor([]);

            if (constructor is null)
            {
                throw new MissingMethodException($"Cannot find default constructor for {typeof(T)}.");
            }

            CasAssemblyLoader.AssertCanCall(Assembly.GetCallingAssembly(), null, constructor);
        }

        return Activator.CreateInstance<T>();
    }

    private static object? CreateInstance(Assembly assembly, Type type, BindingFlags bindingAttr, Binder? binder, object?[]? args, CultureInfo? culture, object?[]? activationAttributes)
    {
        if (!type.IsPrimitive)
        {
            ArgumentNullException.ThrowIfNull(type);
            var constructors = type.GetConstructors(bindingAttr)
                .Where(x => ArgumentsBindable(x, args));

            foreach (var constructor in constructors)
            {
                CasAssemblyLoader.AssertCanCall(assembly, null, constructor);
            }
        }

        return Activator.CreateInstance(type, bindingAttr, binder, args, culture, null);
    }

    private static object? CreateInstance(Assembly assembly, Type type, bool nonPublic)
    {
        if (!type.IsPrimitive)
        {
            ArgumentNullException.ThrowIfNull(type);
            var constructor = type.GetConstructor([]);

            if (constructor is null)
            {
                throw new MissingMethodException($"Cannot find default constructor for {type}.");
            }

            CasAssemblyLoader.AssertCanCall(assembly, null, constructor);
        }

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

    /** System.Runtime.CompilerServices.RuntimeHelpers shims **/

    [StaticShim(typeof(RuntimeHelpers))]
    public static void InitializeArray(Array array, RuntimeFieldHandle fldHandle)
    {
        var field = FieldInfo.GetFieldFromHandle(fldHandle);
        CasAssemblyLoader.AssertCanAccess(Assembly.GetCallingAssembly(), field);
        RuntimeHelpers.InitializeArray(array, fldHandle);
    }

    /** System.Reflection shims **/

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

    public static Delegate CreateDelegate(MethodInfo target, Type delegateType) => CheckAndReturnDelegate(Assembly.GetCallingAssembly(), target.CreateDelegate(delegateType));
    
    public static Delegate CreateDelegate(MethodInfo target, Type delegateType, object? targetObj) => CheckAndReturnDelegate(Assembly.GetCallingAssembly(), target.CreateDelegate(delegateType, targetObj));
    
    public static T CreateDelegate<T>(MethodInfo target) where T : Delegate => CheckAndReturnDelegate(Assembly.GetCallingAssembly(), target.CreateDelegate<T>());

    public static T CreateDelegate<T>(MethodInfo target, object? targetObj) where T : Delegate => CheckAndReturnDelegate(Assembly.GetCallingAssembly(), target.CreateDelegate<T>(targetObj));

    internal static bool TryGetShim(MethodInfo target, [NotNullWhen(true)] out MethodInfo? result)
    {
        var baseDeclaration = target.GetBaseDefinition();
        if (target.IsConstructedGenericMethod)
        {
            baseDeclaration = target.GetGenericMethodDefinition();
        }

        if (ReplacementShims.TryGetValue(baseDeclaration, out MethodBase? shim))
        {
            if (target.IsConstructedGenericMethod)
            {
                result = ((MethodInfo)shim).MakeGenericMethod(target.GetGenericArguments());
            }
            else
            {
                result = (MethodInfo)shim;
            }
            return true;
        }
        else
        {
            result = null;
            return false;
        }
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