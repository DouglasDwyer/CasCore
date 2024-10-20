using System.Security;

namespace DouglasDwyer.CasCore.Tests;

public static class TestDelegateCreation
{
    delegate bool Harmless(object? lhs, object? rhs);
    delegate void FileWriteAllBytes(string name, byte[] contents);

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
    public static void TestHarmless()
    {
        Harmless deleg = ReferenceEquals;
        deleg(2, 3);

        deleg = (Harmless)Delegate.CreateDelegate(typeof(Harmless), typeof(object), "ReferenceEquals");
        deleg(3, 4);
    }
}