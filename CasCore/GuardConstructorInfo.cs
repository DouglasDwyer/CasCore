using System.Globalization;
using System.Reflection;

namespace DouglasDwyer.CasCore;

/// <summary>
/// Wraps a <see cref="ConstructorInfo"/> and prevents it
/// from being called without proper permissions.
/// </summary>
internal class GuardConstructorInfo : ConstructorInfo
{
    /// <summary>
    /// The assembly that created this constructor info.
    /// </summary>
    private readonly Assembly _assembly;

    /// <summary>
    /// The actual constructor.
    /// </summary>
    private readonly ConstructorInfo _inner;

    /// <inheritdoc/>
    public override MethodAttributes Attributes => _inner.Attributes;

    /// <inheritdoc/>
    public override RuntimeMethodHandle MethodHandle => throw new NotSupportedException();

    /// <inheritdoc/>
    public override Type? DeclaringType => _inner.DeclaringType;

    /// <inheritdoc/>
    public override string Name => _inner.Name;

    /// <inheritdoc/>
    public override Type? ReflectedType => _inner.ReflectedType;

    /// <summary>
    /// Creates a new guarded constructor.
    /// </summary>
    /// <param name="assembly">The assembly accessing the constructor.</param>
    /// <param name="inner">The constructor to wrap.</param>
    private GuardConstructorInfo(Assembly assembly, ConstructorInfo inner)
    {
        _assembly = assembly;
        _inner = inner;
    }

    /// <summary>
    /// Creates a new guarded constructor info if necessary.
    /// If the provided constructor can always be called, then the original
    /// <see cref="ConstructorInfo"/> is returned.
    /// </summary>
    /// <param name="assembly">The assembly attempting to access the constructor.</param>
    /// <param name="inner">The constructor to guard.</param>
    /// <returns>A constructor info that only functions with the proper permissions.</returns>
    public static ConstructorInfo Create(Assembly assembly, ConstructorInfo inner)
    {
        if (CasAssemblyLoader.CanCallAlways(assembly, inner))
        {
            return inner;
        }
        else
        {
            return new GuardConstructorInfo(assembly, inner);
        }
    }

    /// <inheritdoc/>
    public override object[] GetCustomAttributes(bool inherit)
    {
        return _inner.GetCustomAttributes(inherit);
    }

    /// <inheritdoc/>
    public override object[] GetCustomAttributes(Type attributeType, bool inherit)
    {
        return _inner.GetCustomAttributes(attributeType, inherit);
    }

    /// <inheritdoc/>
    public override MethodImplAttributes GetMethodImplementationFlags()
    {
        return _inner.GetMethodImplementationFlags();
    }

    /// <inheritdoc/>
    public override ParameterInfo[] GetParameters()
    {
        return _inner.GetParameters();
    }

    /// <inheritdoc/>
    public override object? Invoke(object? obj, BindingFlags invokeAttr, Binder? binder, object?[]? parameters, CultureInfo? culture)
    {
        CasAssemblyLoader.CheckVirtualCall(_assembly, obj, _inner);
        return _inner.Invoke(obj, invokeAttr, binder, parameters, culture);
    }

    /// <inheritdoc/>
    public override bool IsDefined(Type attributeType, bool inherit)
    {
        return _inner.IsDefined(attributeType, inherit);
    }

    /// <inheritdoc/>
    public override object Invoke(BindingFlags invokeAttr, Binder? binder, object?[]? parameters, CultureInfo? culture)
    {
        CasAssemblyLoader.CheckVirtualCall(_assembly, null, _inner);
        return _inner.Invoke(invokeAttr, binder, parameters, culture);
    }
}