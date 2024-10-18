using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace DouglasDwyer.CasCore;

internal unsafe static class LateBindingResolver
{
    private static delegate*<QCallTypeHandle, QCallTypeHandle, IntPtr, IntPtr> GetInterfaceMethodImplementation { get; } = 
        (delegate*<QCallTypeHandle, QCallTypeHandle, IntPtr, IntPtr>)typeof(RuntimeTypeHandle)
            .GetMethod("GetInterfaceMethodImplementation", BindingFlags.NonPublic | BindingFlags.Static)!
            .MethodHandle
            .GetFunctionPointer();

    private static delegate*<IntPtr, Type, IntPtr> GetMethodFromCanonical { get; } =
        (delegate*<IntPtr, Type, IntPtr>)typeof(RuntimeMethodHandle)
            .GetMethod("GetMethodFromCanonical", BindingFlags.NonPublic | BindingFlags.Static)!
            .MethodHandle
            .GetFunctionPointer();

    public static MethodBase GetTargetMethod(object? obj, MethodBase method)
    {
        unsafe
        {
            if (obj is not null && method is MethodInfo info && method.IsVirtual)
            {
                var objType = obj.GetType();
                if (objType.IsSZArray)
                {
                    return GetTargetMethodViaDelegate(obj, info, info.GetParameters().Select(x => x.ParameterType));
                }
                else
                {
                    return GetTargetMethodViaPInvoke(obj, objType, info);
                }
            }
            else
            {
                return method;
            }
        }
    }

    private static MethodBase GetTargetMethodViaPInvoke(object obj, Type objType, MethodInfo info)
    {
        var typeHandle = objType.TypeHandle;
        IntPtr methodPtr;
        if (info.DeclaringType!.IsInterface)
        {
            var interfaceHandle = info.DeclaringType.TypeHandle;
            methodPtr = GetInterfaceMethodImplementation(
                new QCallTypeHandle(ref typeHandle),
                new QCallTypeHandle(ref interfaceHandle),
                info.MethodHandle.Value);
        }
        else
        {
            methodPtr = GetMethodFromCanonical(info.MethodHandle.Value, objType);
        }

        return MethodBase.GetMethodFromHandle(RuntimeMethodHandle.FromIntPtr(methodPtr), typeHandle)!;
    }

    /// <summary>
    /// Obtains a target method handle by creating a delegate which will automatically perform method resolution.
    /// This method may only be called if there are less than 15 parameters, none of which may be by-refs.
    /// </summary>
    /// <param name="obj">The target object to call.</param>
    /// <param name="info">The method that will be called on the object.</param>
    /// <param name="parameters">The parameter types of the given method.</param>
    /// <returns>The method that will actually be called on the object.</returns>
    private static MethodBase GetTargetMethodViaDelegate(object obj, MethodInfo info, IEnumerable<Type> parameters)
    {
        Type delegateType;
        if (info.ReturnType.Equals(typeof(void)))
        {
            delegateType = Expression.GetActionType(parameters.ToArray());
        }
        else
        {
            delegateType = Expression.GetFuncType(parameters.Append(info.ReturnType).ToArray());
        }

        return Delegate.CreateDelegate(delegateType, obj, info).Method;
    }

    private unsafe ref struct QCallTypeHandle
    {
        private void* _ptr;
        private IntPtr _handle;

        internal QCallTypeHandle(ref Type type)
        {
            _ptr = Unsafe.AsPointer(ref type);
            _handle = type?.TypeHandle.Value ?? IntPtr.Zero;
        }

        internal QCallTypeHandle(ref RuntimeTypeHandle rth)
        {
            _ptr = Unsafe.AsPointer(ref rth);
            _handle = rth.Value;
        }
    }
}