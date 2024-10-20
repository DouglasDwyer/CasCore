namespace DouglasDwyer.CasCore.Tests.Shared;

public class SharedClass
{
    public static int AllowedStaticField = 29;
    public static int DeniedStaticField = 30;

    public int AllowedField = 1;
    public int DeniedField = 4;

    public int DeniedProperty { get; } = 20;

    public SharedClass() { }

    public SharedClass(string denied) { }
}