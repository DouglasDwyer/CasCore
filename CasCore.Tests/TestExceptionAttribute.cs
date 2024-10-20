namespace DouglasDwyer.CasCore.Tests;

public class TestExceptionAttribute : Attribute
{
    public Type ExpectedException { get; }

    public TestExceptionAttribute(Type exception)
    {
        ExpectedException = exception;
    }
}