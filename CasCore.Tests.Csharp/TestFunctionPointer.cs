using System.Runtime.InteropServices;
using System.Security;
using DouglasDwyer.CasCore.Tests.Shared;

namespace DouglasDwyer.CasCore.Tests.Csharp;

public static class TestFunctionPointer
{
    [TestException(typeof(SecurityException))]
    public static void TestFunctionPointerCannotBeUsedViaMarshal()
    {
        var method = typeof(SharedClass).GetMethod(nameof(SharedClass.VirtualMethod))!;
        var ptr = method.MethodHandle.GetFunctionPointer();
        // Marshal is not in the default policy — this should throw SecurityException.
        Marshal.GetDelegateForFunctionPointer<Action>(ptr);
    }
}
