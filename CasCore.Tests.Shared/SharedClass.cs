namespace DouglasDwyer.CasCore.Tests.Shared;

public class SharedClass : ISharedInterface
{
    public static int AllowedStaticField = 29;
    public static int DeniedStaticField = 30;

    public int AllowedField = 1;
    public int DeniedField = 4;

    public int DeniedProperty { get; } = 20;

    public SharedClass() { }

    public SharedClass(string denied) { }

    public virtual void VirtualMethod() { }

    public T InterfaceMethod<T>(T input)
    {
        return input;
    }

    public class SharedNested : SharedClass, ISharedInterface
    {
        public override void VirtualMethod() { }

        public new T InterfaceMethod<T>(T input)
        {
            return input;
        }
    }
}