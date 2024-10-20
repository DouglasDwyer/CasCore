using System.Globalization;
using System.Reflection;

namespace DouglasDwyer.CasCore;

internal class GuardPropertyInfo : PropertyInfo
{
    private readonly Assembly _assembly;
    private readonly PropertyInfo _inner;

    public override PropertyAttributes Attributes => _inner.Attributes;

    public override bool CanRead => _inner.CanRead;

    public override bool CanWrite => _inner.CanWrite;

    public override Type PropertyType => _inner.PropertyType;

    public override Type? DeclaringType => _inner.DeclaringType;

    public override string Name => _inner.Name;

    public override Type? ReflectedType => _inner.ReflectedType;

    private GuardPropertyInfo(Assembly assembly, PropertyInfo inner)
    {
        _assembly = assembly;
        _inner = inner;
    }

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

    public override MethodInfo[] GetAccessors(bool nonPublic)
    {
        return _inner.GetAccessors(nonPublic).Select(x => GuardMethodInfo.Create(_assembly, x)).ToArray();
    }

    public override object[] GetCustomAttributes(bool inherit)
    {
        return _inner.GetCustomAttributes(inherit);
    }

    public override object[] GetCustomAttributes(Type attributeType, bool inherit)
    {
        return _inner.GetCustomAttributes(attributeType, inherit);
    }

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

    public override ParameterInfo[] GetIndexParameters()
    {
        return _inner.GetIndexParameters();
    }

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

    public override object? GetValue(object? obj, BindingFlags invokeAttr, Binder? binder, object?[]? index, CultureInfo? culture)
    {
        if (CanRead)
        {
            CasAssemblyLoader.AssertCanCall(_assembly, obj, _inner.GetMethod!);
        }

        return _inner.GetValue(obj, invokeAttr, binder, index, culture);
    }

    public override bool IsDefined(Type attributeType, bool inherit)
    {
        throw new NotImplementedException();
    }

    public override void SetValue(object? obj, object? value, BindingFlags invokeAttr, Binder? binder, object?[]? index, CultureInfo? culture)
    {
        if (CanWrite)
        {
            CasAssemblyLoader.AssertCanCall(_assembly, obj, _inner.SetMethod!);
        }

        _inner.SetValue(obj, value, invokeAttr, binder, index, culture);
    }
}