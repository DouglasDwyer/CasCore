using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace DouglasDwyer.CasCore;

/// <summary>
/// A helper class for determining which method will <b>actually</b> be
/// invoked as the result of a virtual call.
/// </summary>
internal unsafe static class LateBindingResolver
{
    /// <summary>
    /// Gets the handle of the method that would actually be invoked
    /// as the result of a call to an interface.
    /// </summary>
    private static delegate*<QCallTypeHandle, QCallTypeHandle, IntPtr, IntPtr> GetInterfaceMethodImplementation { get; } = 
        (delegate*<QCallTypeHandle, QCallTypeHandle, IntPtr, IntPtr>)typeof(RuntimeTypeHandle)
            .GetMethod("GetInterfaceMethodImplementation", BindingFlags.NonPublic | BindingFlags.Static)!
            .MethodHandle
            .GetFunctionPointer();

    /// <summary>
    /// Gets the handle of the method that would actually be invoked
    /// as the result of a call to a virtual method.
    /// </summary>
    private static delegate*<IntPtr, Type, IntPtr> GetMethodFromCanonical { get; } =
        (delegate*<IntPtr, Type, IntPtr>)typeof(RuntimeMethodHandle)
            .GetMethod("GetMethodFromCanonical", BindingFlags.NonPublic | BindingFlags.Static)!
            .MethodHandle
            .GetFunctionPointer();

    /// <summary>
    /// Gets the method that will actually be invoked for a virtual call.
    /// </summary>
    /// <param name="obj">The object for the virtual call.</param>
    /// <param name="method">The method being invoked.</param>
    /// <returns>The actual method that would be called.</returns>
    public static MethodBase GetTargetMethod(object? obj, MethodBase method)
    {
        unsafe
        {
            if (obj is null && !method.IsStatic)
            {
                throw new NullReferenceException();
            }

            else if (obj is not null && method is MethodInfo info && method.IsVirtual)
            {
                var objType = obj.GetType();
                if (objType.IsSZArray)
                {
                    return GetTargetMethodViaDelegate(obj, info, info.GetParameters().Select(x => x.ParameterType));
                }
                else
                {
                    return GetTargetMethodViaPInvoke(objType, info);
                }
            }
            else
            {
                return method;
            }
        }
    }

    /// <summary>
    /// Gets the method that will actually be invoked for the given type
    /// by calling some internal CLR methods.
    /// </summary>
    /// <param name="objType">The actual type on which the method is being invoked.</param>
    /// <param name="info">The virtual method that is being invoked.</param>
    /// <returns>The target method that will actually be called.</returns>
    private static MethodBase GetTargetMethodViaPInvoke(Type objType, MethodInfo info)
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
    
    /// <summary>
    /// A helper structure for method calls used by the CLR internally.
    /// </summary>
    private unsafe ref struct QCallTypeHandle
    {
        /// <summary>
        /// A pointer to the type being invoked.
        /// </summary>
        private void* _ptr;

        /// <summary>
        /// The handle of the type being invoked.
        /// </summary>
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