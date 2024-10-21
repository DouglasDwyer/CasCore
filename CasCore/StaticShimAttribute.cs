namespace DouglasDwyer.CasCore;

/// <summary>
/// Identifies a shim as referring to a static method.
/// </summary>
internal class StaticShimAttribute : Attribute
{
    /// <summary>
    /// The target type containing the method that this shim replaces.
    /// </summary>
    public Type Target { get; }

    /// <summary>
    /// Marks a shim as replacing a static method.
    /// </summary>
    /// <param name="target">The target type containing the method that this shim replaces.</param>
    public StaticShimAttribute(Type target)
    {
        Target = target;
    }
}