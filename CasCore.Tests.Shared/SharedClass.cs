namespace DouglasDwyer.CasCore.Tests.Shared;

public class SharedClass
{
    public static int AllowedStaticField = 29;
    public static int DeniedStaticField = 30;

    public SharedClass() { }

    public SharedClass(string denied) { }
}