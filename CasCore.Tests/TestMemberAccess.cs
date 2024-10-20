using DouglasDwyer.CasCore.Tests.Shared;
using System.Security;

namespace DouglasDwyer.CasCore.Tests;

public static class TestMemberAccess
{
    [TestException(typeof(SecurityException))]
    public static void TestAccessDeniedStatic()
    {
        var x = SharedClass.DeniedStaticField;
    }

    [TestSuccessful]
    public static void TestAccessAllowedStatic()
    {
        var x = SharedClass.AllowedStaticField;
    }

    [TestException(typeof(SecurityException))]
    public static void TestAccessDeniedConstructor()
    {
        var x = new SharedClass("hello");
    }

    [TestSuccessful]
    public static void TestAccessAllowedConstructor()
    {
        var x = new SharedClass();
    }

    [TestException(typeof(SecurityException))]
    public static void TestAccessDenied()
    {
        var instance = new SharedClass();
        var x = instance.DeniedField;
    }

    [TestSuccessful]
    public static void TestAccessAllowed()
    {
        var instance = new SharedClass();
        var x = instance.AllowedField;
    }

    [TestException(typeof(SecurityException))]
    public static void TestAccessDeniedVirtualMethod()
    {
        CallVirtualMethod(new SharedClass());
    }

    [TestSuccessful]
    public static void TestAccessAllowedVirtualMethod()
    {
        CallVirtualMethod(new SharedClass.SharedNested());
    }

    [TestSuccessful]
    public static void TestAccessAllowedInterfaceMethod()
    {
        CallInterfaceMethod(new SharedClass(), 29);
    }

    [TestException(typeof(SecurityException))]
    public static void TestAccessDeniedInterfaceMethod()
    {
        CallInterfaceMethod(new SharedClass.SharedNested(), 29);
    }

    private static void CallVirtualMethod(SharedClass shared)
    {
        shared.VirtualMethod();
    }

    private static void CallInterfaceMethod<T>(ISharedInterface shared, T value)
    {
        shared.InterfaceMethod(value);
    }
}