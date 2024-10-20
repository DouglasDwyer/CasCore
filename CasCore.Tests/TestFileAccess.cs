using System.Security;
using System.Text;

namespace DouglasDwyer.CasCore.Tests;

public static class TestFileAccess
{
    [TestException(typeof(SecurityException))]
    public static void TestReadFile()
    {
        File.ReadAllText("hello.txt");
    }

    [TestException(typeof(SecurityException))]
    public static void TestWriteFile()
    {
        File.WriteAllText("hello.txt", "contents");
    }

    [TestException(typeof(SecurityException))]
    public static void TestStreamReaderFileConstructor1()
    {
        new StreamReader("hello.txt");
    }

    [TestException(typeof(SecurityException))]
    public static void TestStreamReaderFileConstructor2()
    {
        new StreamReader("hello.txt", false);
    }

    [TestException(typeof(SecurityException))]
    public static void TestStreamReaderFileConstructor3()
    {
        new StreamReader("hello.txt", Encoding.UTF8);
    }

    [TestSuccessful]
    public static void TestStreamReaderOtherConstructors()
    {
        new StreamReader(new MemoryStream());
        new StreamReader(new MemoryStream(), true);
        new StreamReader(new MemoryStream(), Encoding.ASCII);
    }

    [TestException(typeof(SecurityException))]
    public static void TestStreamWriterFileConstructor1()
    {
        new StreamWriter("hello.txt");
    }

    [TestException(typeof(SecurityException))]
    public static void TestStreamWriterFileConstructor2()
    {
        new StreamWriter("hello.txt", false);
    }

    [TestException(typeof(SecurityException))]
    public static void TestStreamWriterFileConstructor3()
    {
        new StreamWriter("hello.txt", true, Encoding.UTF8, 200);
    }

    [TestSuccessful]
    public static void TestStreamWriterOtherConstructors()
    {
        new StreamWriter(new MemoryStream());
        new StreamWriter(new MemoryStream(), Encoding.ASCII);
    }

    [TestException(typeof(SecurityException))]
    public static void TestEnvironmentGetFolderPath()
    {
        Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
    }

    [TestException(typeof(SecurityException))]
    public static void TestEnvironmentGetCommandLineArgs()
    {
        Environment.GetCommandLineArgs();
    }

    [TestException(typeof(SecurityException))]
    public static void TestEnvironmentGetLogicalDrives()
    {
        Environment.GetLogicalDrives();
    }

    [TestException(typeof(SecurityException))]
    public static void TestEnvironmentProcessPath()
    {
        var x = Environment.ProcessPath;
    }
}