using DouglasDwyer.CasCore.Tests.Shared;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security;

namespace DouglasDwyer.CasCore.Tests;

public static class TestReflection
{
    [TestException(typeof(SecurityException))]
    public static object? TestAccessDeniedStatic()
    {
        var fieldInfo = typeof(SharedClass).GetField("DeniedStaticField")!;
        return fieldInfo.GetValue(null);
    }

    [TestSuccessful]
    public static object? TestAccessAllowedStatic()
    {
        var fieldInfo = typeof(SharedClass).GetField("AllowedStaticField")!;
        return fieldInfo.GetValue(null);
    }

    [TestException(typeof(SecurityException))]
    public static object TestAccessDeniedConstructor()
    {
        var constructorInfo = typeof(SharedClass).GetConstructor([typeof(string)])!;
        return constructorInfo.Invoke(["test"]);
    }

    [TestSuccessful]
    public static object TestAccessAllowedConstructor()
    {
        var constructorInfo = typeof(SharedClass).GetConstructor([])!;
        return constructorInfo.Invoke(null);
    }

    [TestException(typeof(SecurityException))]
    public static object? TestAccessDenied()
    {
        var instance = new SharedClass();
        var fieldInfo = typeof(SharedClass).GetField("DeniedField")!;
        return fieldInfo.GetValue(instance);
    }

    [TestSuccessful]
    public static object? TestAccessAllowed()
    {
        var instance = new SharedClass();
        var fieldInfo = typeof(SharedClass).GetField("AllowedField")!;
        return fieldInfo.GetValue(instance);
    }

    [TestException(typeof(SecurityException))]
    public static void TestAccessDeniedVirtualMethod()
    {
        var method = typeof(SharedClass).GetMethod("VirtualMethod")!;
        method.Invoke(new SharedClass(), null);
    }

    [TestSuccessful]
    public static void TestAccessAllowedVirtualMethod()
    {
        var method = typeof(SharedClass).GetMethod("VirtualMethod")!;
        method.Invoke(new SharedClass.SharedNested(), null);
    }

    [TestSuccessful]
    public static void TestAccessAllowedInterfaceMethod()
    {
        var method = typeof(ISharedInterface).GetMethod("InterfaceMethod")!.MakeGenericMethod([typeof(int)]);
        method.Invoke(new SharedClass(), [29]);
    }

    [TestException(typeof(SecurityException))]
    public static void TestAccessDeniedInterfaceMethod()
    {
        var method = typeof(ISharedInterface).GetMethod("InterfaceMethod")!.MakeGenericMethod([typeof(int)]);
        method.Invoke(new SharedClass.SharedNested(), [29]);
    }

    [TestException(typeof(SecurityException))]
    public static void TestAccessDeniedRecursiveInvoke()
    {
        var method = typeof(ISharedInterface).GetMethod("InterfaceMethod")!.MakeGenericMethod([typeof(int)]);
        var methodInvoke = method.GetType().GetMethod("Invoke", BindingFlags.Instance | BindingFlags.Public, [typeof(object), typeof(object[])])!;
        methodInvoke.Invoke(method, [new SharedClass.SharedNested(), new object[] { 29 }]);
    }

    [TestSuccessful]
    public static void TestAccessAllowedGetReadonly()
    {
        var staticField = typeof(SharedClass).GetField(nameof(SharedClass.AllowedReadonlyStaticField))!;
        var instanceField = typeof(SharedClass).GetField(nameof(SharedClass.AllowedField))!;

        var staticValue = staticField.GetValue(null);
        var instanceValue = instanceField.GetValue(new SharedClass());
    }

    [TestException(typeof(FieldAccessException))]
    public static void TestAccessDeniedSetStaticReadonly()
    {
        var staticField = typeof(SharedClass).GetField(nameof(SharedClass.AllowedReadonlyStaticField))!;
        staticField.SetValue(null, 90);
    }

    [TestException(typeof(SecurityException))]
    public static void TestAccessDeniedSetInstanceReadonly()
    {
        var instanceField = typeof(SharedClass).GetField(nameof(SharedClass.AllowedField))!;
        instanceField.SetValue(new SharedClass(), 91);
    }

    [TestSuccessful]
    public static void TestAccessDeniedInvokeCtorWithoutExisting()
    {
        var constructor = typeof(SharedClass).GetConstructor([])!;
        constructor.Invoke(null);
    }

    [TestException(typeof(SecurityException))]
    public static void TestAccessDeniedInvokeCtorWithExisting()
    {
        var constructor = typeof(SharedClass).GetConstructor([])!;
        var obj = new SharedClass();
        constructor.Invoke(obj, null);
    }

    [TestException(typeof(SecurityException))]
    public static void TestAccessDeniedGetUninitializedObject()
    {
        RuntimeHelpers.GetUninitializedObject(typeof(SharedClass));
    }
}