namespace DouglasDwyer.CasCore;

internal class StaticShimAttribute : Attribute
{
    public Type Target { get; }

    public StaticShimAttribute(Type target)
    {
        Target = target;
    }
}