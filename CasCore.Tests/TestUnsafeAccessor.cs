using System.Runtime.CompilerServices;
using System.Security;

namespace DouglasDwyer.CasCore.Tests;

public static class TestUnsafeAccessors
{
    [TestException(typeof(InvalidProgramException))]
    public static void TestNoUnsafeAccessors()
    {
        var foo = new Foo();

        string hidden = GetHiddenField(foo);
        
        if (hidden == "foo")
        {
            throw new Exception("Test failed");
        }
    }

    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_hiddenField")]
    private static extern string GetHiddenField(Foo person);

    private class Foo
    {
        public string HiddenValue => _hiddenField;

        private string _hiddenField = "foo";
    }
}