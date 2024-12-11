using System.Globalization;
using System.Reflection;

namespace DouglasDwyer.CasCore;

/// <summary>
/// Wraps a <see cref="MethodInfo"/> and prevents it
/// from being called without proper permissions.
/// </summary>
internal class GuardMethodInfo : MethodInfo
{
    /// <summary>
    /// The assembly that created this method info.
    /// </summary>
    private readonly Assembly _assembly;

    /// <summary>
    /// The actual method.
    /// </summary>
    private readonly MethodInfo _inner;

    /// <inheritdoc/>
    public override ICustomAttributeProvider ReturnTypeCustomAttributes => _inner.ReturnTypeCustomAttributes;

    /// <inheritdoc/>
    public override MethodAttributes Attributes => _inner.Attributes;

    /// <inheritdoc/>
    public override RuntimeMethodHandle MethodHandle => throw new NotSupportedException();

    /// <inheritdoc/>
    public override ParameterInfo ReturnParameter => _inner.ReturnParameter;

    /// <inheritdoc/>
    public override Type ReturnType => _inner.ReturnType;

    /// <inheritdoc/>
    public override Type? DeclaringType => _inner.DeclaringType;

    /// <inheritdoc/>
    public override string Name => _inner.Name;

    /// <inheritdoc/>
    public override Type? ReflectedType => _inner.ReflectedType;

    /// <summary>
    /// Creates a new guarded method.
    /// </summary>
    /// <param name="assembly">The assembly accessing the method.</param>
    /// <param name="inner">The method to wrap.</param>
    private GuardMethodInfo(Assembly assembly, MethodInfo inner)
    {
        _assembly = assembly;
        _inner = inner;
    }
    /// <summary>
    /// Creates a new guarded method info if necessary.
    /// If the provided method can always be called, then the original
    /// <see cref="MethodInfo"/> is returned.
    /// </summary>
    /// <param name="assembly">The assembly attempting to access the method.</param>
    /// <param name="inner">The method to guard.</param>
    /// <returns>A method info that only functions with the proper permissions.</returns>

    public static MethodInfo Create(Assembly assembly, MethodInfo inner)
    {
        if (CasAssemblyLoader.CanCallAlways(assembly, inner))
        {
            return inner;
        }
        else
        {
            return new GuardMethodInfo(assembly, inner);
        }
    }

    /// <inheritdoc/>
    public override MethodInfo GetBaseDefinition()
    {
        throw new NotSupportedException();
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
}