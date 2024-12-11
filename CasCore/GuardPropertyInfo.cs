using System.Globalization;
using System.Reflection;

namespace DouglasDwyer.CasCore;

/// <summary>
/// Wraps a <see cref="PropertyInfo"/> and prevents its methods
/// from being called without proper permissions.
/// </summary>
internal class GuardPropertyInfo : PropertyInfo
{
    /// <summary>
    /// The assembly that created this property info.
    /// </summary>
    private readonly Assembly _assembly;

    /// <summary>
    /// The actual property.
    /// </summary>
    private readonly PropertyInfo _inner;

    /// <inheritdoc/>
    public override PropertyAttributes Attributes => _inner.Attributes;

    /// <inheritdoc/>
    public override bool CanRead => _inner.CanRead;

    /// <inheritdoc/>
    public override bool CanWrite => _inner.CanWrite;

    /// <inheritdoc/>
    public override Type PropertyType => _inner.PropertyType;

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

    private GuardPropertyInfo(Assembly assembly, PropertyInfo inner)
    {
        _assembly = assembly;
        _inner = inner;
    }

    /// <summary>
    /// Creates a new guarded property info if necessary.
    /// If the provided property can always be called, then the original
    /// <see cref="PropertyInfo"/> is returned.
    /// </summary>
    /// <param name="assembly">The assembly attempting to access the property.</param>
    /// <param name="inner">The property to guard.</param>
    /// <returns>A property info that only functions with the proper permissions.</returns>
    public static PropertyInfo Create(Assembly assembly, PropertyInfo inner)
    {
        var readAllowed = inner.GetMethod is null || CasAssemblyLoader.CanCallAlways(assembly, inner.GetMethod);
        var writeAllowed = inner.SetMethod is null || CasAssemblyLoader.CanCallAlways(assembly, inner.SetMethod);

        if (readAllowed && writeAllowed)
        {
            return inner;
        }
        else
        {
            return new GuardPropertyInfo(assembly, inner);
        }
    }

    /// <inheritdoc/>
    public override MethodInfo[] GetAccessors(bool nonPublic)
    {
        return _inner.GetAccessors(nonPublic).Select(x => GuardMethodInfo.Create(_assembly, x)).ToArray();
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
    public override MethodInfo? GetGetMethod(bool nonPublic)
    {
        var result = _inner.GetGetMethod(nonPublic);
        if (result is null)
        {
            return result;
        }
        else
        {
            return GuardMethodInfo.Create(_assembly, result);
        }
    }

    /// <inheritdoc/>
    public override ParameterInfo[] GetIndexParameters()
    {
        return _inner.GetIndexParameters();
    }

    /// <inheritdoc/>
    public override MethodInfo? GetSetMethod(bool nonPublic)
    {
        var result = _inner.GetSetMethod(nonPublic);
        if (result is null)
        {
            return result;
        }
        else
        {
            return GuardMethodInfo.Create(_assembly, result);
        }
    }

    /// <inheritdoc/>
    public override object? GetValue(object? obj, BindingFlags invokeAttr, Binder? binder, object?[]? index, CultureInfo? culture)
    {
        if (CanRead)
        {
            CasAssemblyLoader.CheckVirtualCall(_assembly, obj, _inner.GetMethod!);
        }

        return _inner.GetValue(obj, invokeAttr, binder, index, culture);
    }

    /// <inheritdoc/>
    public override bool IsDefined(Type attributeType, bool inherit)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public override void SetValue(object? obj, object? value, BindingFlags invokeAttr, Binder? binder, object?[]? index, CultureInfo? culture)
    {
        if (CanWrite)
        {
            CasAssemblyLoader.CheckVirtualCall(_assembly, obj, _inner.SetMethod!);
        }

        _inner.SetValue(obj, value, invokeAttr, binder, index, culture);
    }
}