using System.Security;
using DouglasDwyer.CasCore.Tests.Shared;

namespace DouglasDwyer.CasCore.Tests.Csharp;

public static class TestDelegateCreation
{
    delegate bool Harmless(object? lhs, object? rhs);
    delegate void FileWriteAllBytes(string name, byte[] contents);

    static event Action? MyEvent;

    [TestException(typeof(SecurityException))]
    public static void TestWriteFile()
    {
        FileWriteAllBytes deleg = File.WriteAllBytes;
        deleg("hello.txt", [1, 2, 3]);
    }

    [TestException(typeof(SecurityException))]
    public static void TestWriteFileCreateDelegate()
    {
        var deleg = (FileWriteAllBytes)Delegate.CreateDelegate(typeof(FileWriteAllBytes), typeof(File), "WriteAllBytes");
        deleg("hello.txt", [1, 2, 3]);
    }

    [TestSuccessful]
    public static void TestEventSubscription()
    {
        MyEvent += () => { };
        MyEvent();
    }

    [TestException(typeof(SecurityException))]
    public static void TestDeniedVirtualMethodDelegate()
    {
        var obj = new SharedClass();
        var del = new Action(obj.VirtualMethod);
        del();
    }

    [TestSuccessful]
    public static void TestHarmless()
    {
        Harmless deleg = ReferenceEquals;
        deleg(2, 3);

        deleg = (Harmless)Delegate.CreateDelegate(typeof(Harmless), typeof(object), "ReferenceEquals");
        deleg(3, 4);
    }
}