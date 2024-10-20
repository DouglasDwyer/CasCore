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
}