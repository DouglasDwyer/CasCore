using System.Globalization;
using System.Reflection;

namespace DouglasDwyer.CasCore;

internal class GuardMethodInfo : MethodInfo
{
    private readonly Assembly _assembly;
    private readonly MethodInfo _inner;

    public override ICustomAttributeProvider ReturnTypeCustomAttributes => _inner.ReturnTypeCustomAttributes;

    public override MethodAttributes Attributes => _inner.Attributes;

    public override RuntimeMethodHandle MethodHandle => throw new NotSupportedException();

    public override Type? DeclaringType => _inner.DeclaringType;

    public override string Name => _inner.Name;

    public override Type? ReflectedType => _inner.ReflectedType;

    private GuardMethodInfo(Assembly assembly, MethodInfo inner)
    {
        _assembly = assembly;
        _inner = inner;
    }

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

    public override MethodInfo GetBaseDefinition()
    {
        throw new NotSupportedException();
    }

    public override object[] GetCustomAttributes(bool inherit)
    {
        return _inner.GetCustomAttributes(inherit);
    }

    public override object[] GetCustomAttributes(Type attributeType, bool inherit)
    {
        return _inner.GetCustomAttributes(attributeType, inherit);
    }

    public override MethodImplAttributes GetMethodImplementationFlags()
    {
        return _inner.GetMethodImplementationFlags();
    }

    public override ParameterInfo[] GetParameters()
    {
        return _inner.GetParameters();
    }

    public override object? Invoke(object? obj, BindingFlags invokeAttr, Binder? binder, object?[]? parameters, CultureInfo? culture)
    {
        CasAssemblyLoader.AssertCanCall(_assembly, obj, _inner);
        return _inner.Invoke(obj, invokeAttr, binder, parameters, culture);
    }

    public override bool IsDefined(Type attributeType, bool inherit)
    {
        return _inner.IsDefined(attributeType, inherit);
    }
}