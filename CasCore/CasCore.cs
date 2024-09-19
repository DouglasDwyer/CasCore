using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Security;
using static CasCore.CasPolicy;

namespace CasCore;

[Flags]
public enum CasBindingFlags
{
    Default = 0,
    DeclaredOnly = 2,
    Instance = 4,
    Static = 8,
    Public = 16,
    NonPublic = 32,
    Field = 64,
    Constructor = 128,
    Method = 256,
    Member = Field | Constructor | Method | Static | Instance,
    All = Public | NonPublic | Member
}

public class CasPermission
{
    public string Name { get; init; }
    public ulong Id { get; init; }

    private static ulong _idCounter = 0;

    public CasPermission(string name)
    {
        Name = name;
        Id = Interlocked.Add(ref _idCounter, 1);
    }

    public override string ToString()
    {
        return Name;
    }
}

public class CasPolicy
{
    internal readonly Dictionary<FieldInfo, CasPermissionSet> _restrictedFields = new Dictionary<FieldInfo, CasPermissionSet>();
    internal readonly Dictionary<MethodBase, CasPermissionSet> _restrictedMethods = new Dictionary<MethodBase, CasPermissionSet>();

    public CasPolicy() { }

    internal CasPolicy(CasPolicy original)
    {
        _restrictedFields = original._restrictedFields.ToDictionary();
        _restrictedMethods = original._restrictedMethods.ToDictionary();
    }

    public CasPolicy With(TypePolicy builder)
    {
        builder.AddTo(this);
        return this;
    }

    public static TypePolicy Type(Type type, CasBindingFlags flags, CasPermission[] permissions)
    {
        return new TypePolicy(type, flags, permissions);
    }

    public class TypePolicy
    {
        private readonly Dictionary<FieldInfo, CasPermissionSet> _restrictedFields = new Dictionary<FieldInfo, CasPermissionSet>();
        private readonly Dictionary<MethodBase, CasPermissionSet> _restrictedMethods = new Dictionary<MethodBase, CasPermissionSet>();
        private readonly Type _type;

        internal TypePolicy(Type type, CasBindingFlags flags, CasPermission[] permissions)
        {
            _type = type;
            var bindingFlags = MemberFlagsOf(flags);

            var permissionSet = new CasPermissionSet(permissions);

            if (flags.HasFlag(CasBindingFlags.Field))
            {
                foreach (var field in _type.GetFields(bindingFlags))
                {
                    _restrictedFields[field] = permissionSet;
                }
            }

            if (flags.HasFlag(CasBindingFlags.Constructor))
            {
                foreach (var constructor in _type.GetConstructors(bindingFlags))
                {
                    _restrictedMethods[constructor] = permissionSet;
                }
            }

            if (flags.HasFlag(CasBindingFlags.Method))
            {
                foreach (var method in _type.GetMethods(bindingFlags))
                {
                    _restrictedMethods[method] = permissionSet;
                }
            }
        }

        public TypePolicy Constructor(Type[] parameters, CasPermission[] permissions)
        {
            var method = _type.GetConstructor(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static, parameters);
            if (method is null)
            {
                throw new ArgumentException($"Could not find constructor to add to policy.");
            }
            _restrictedMethods[method] = new CasPermissionSet(permissions);
            return this;
        }

        public TypePolicy Field(string name, CasPermission[] permissions)
        {
            var field = _type.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
            if (field is null)
            {
                throw new ArgumentException($"Could not find field {name} to add to policy.");
            }
            _restrictedFields[field] = new CasPermissionSet(permissions);
            return this;
        }

        public TypePolicy Method(string name, CasPermission[] permissions)
        {
            var method = _type.GetMethod(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
            if (method is null)
            {
                throw new ArgumentException($"Could not find method {name} to add to policy.");
            }

            _restrictedMethods[method] = new CasPermissionSet(permissions);

            return this;
        }

        public TypePolicy Method(string name, Type[] parameters, CasPermission[] permissions)
        {
            var method = _type.GetMethod(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static, parameters);
            if (method is null)
            {
                throw new ArgumentException($"Could not find method {name} to add to policy.");
            }

            _restrictedMethods[method] = new CasPermissionSet(permissions);

            return this;
        }

        internal void AddTo(CasPolicy policy)
        {
            foreach (var field in _restrictedFields)
            {
                policy._restrictedFields[field.Key] = field.Value;
            }

            foreach (var field in _restrictedMethods)
            {
                policy._restrictedMethods[field.Key] = field.Value;
            }
        }
    }

    private static BindingFlags MemberFlagsOf(CasBindingFlags flags)
    {
        return (BindingFlags)((uint)(flags) & 63) | BindingFlags.DeclaredOnly;
    }
}

public class CasPermissionSet
{
    public IEnumerable<CasPermission> Permissions => _permissions.Values;

    private readonly Dictionary<ulong, CasPermission> _permissions;

    public CasPermissionSet(CasPermission[] permissions)
    {
        _permissions = new Dictionary<ulong, CasPermission>();
        foreach (var permission in permissions)
        {
            _permissions[permission.Id] = permission;
        }
    }

    public bool Has(ulong permissionId)
    {
        return _permissions.ContainsKey(permissionId);
    }

    public override string ToString()
    {
        return string.Join(",", _permissions.Values);
    }
}

public class CasManager
{
    public static CasManager Instance { get; } = new CasManager();

    private bool _appliedReflectionShims = false;
    private CasPolicy _currentPolicy = new CasPolicy();
    private uint _idCounter = 0;
    private object _locker = new object();

    public void Apply(CasPolicy policy)
    {
        lock (_locker)
        {
            if (!_appliedReflectionShims)
            {
                ApplyReflectionShims();
                _appliedReflectionShims = true;
            }

            AssemblyBuilder guardAssy = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName($"CasGuardAssembly_{NextId()}"), AssemblyBuilderAccess.Run);
            ModuleBuilder guardMod = guardAssy.DefineDynamicModule($"CasGuardModule_{NextId()}");
            TypeBuilder guardType = guardMod.DefineType($"CasGuard_{NextId()}", TypeAttributes.NotPublic | TypeAttributes.Class);

            var originalStubs = policy._restrictedMethods.ToDictionary(x => x.Key, x => CreateStubMethod(guardType, x.Key));
            var guards = policy._restrictedMethods.ToDictionary(x => x.Key, x => CreateGuardMethod(guardType, x.Key, originalStubs[x.Key], x.Value));

            Type instantiatedGuard = guardType.CreateType();
            foreach (var method in policy._restrictedMethods)
            {
                var originalStub = guardType.GetMethod(originalStubs[method.Key].Name, BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic)!;
                var guard = guardType.GetMethod(guards[method.Key].Name, BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic)!;

                if (!method.Key.IsGenericMethod && !method.Key.Attributes.HasFlag(MethodAttributes.PinvokeImpl))
                {
                    Replace(originalStub, method.Key);
                    Replace(method.Key, guard);
                }
            }

            _currentPolicy = new CasPolicy(policy);

            // foreach constructor, method: define dummy and guard method with unique names. Once everything is defined, emit type.
            // Go over everything and swap it.
            // Store data so that methods can be swapped back.

            // all currently loaded assys should be granted perms
            // eventual todo: undo previous policy
        }
    }

    private void ApplyReflectionShims()
    {
        ApplyReflectionShim<ConstructorInfo>(nameof(ConstructorInfo.Invoke));
        ApplyReflectionShim<MethodInfo>(nameof(MethodInfo.Invoke));
    }

    private void ApplyReflectionShim<T>(string name)
    {
        ApplyReflectionShim<T>(name, name);
    }

    private void ApplyReflectionShim<T>(string originalName, string name)
    {
        foreach (var stub in typeof(ReflectionStubs).GetMethods(BindingFlags.NonPublic | BindingFlags.Static).Where(x => x.Name == name))
        {
            var stubParameters = stub.GetParameters().Select(x => x.ParameterType).ToArray();
            var method = typeof(T).GetMethod(originalName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly, stubParameters);
            
            if (method is null)
            {
                method = typeof(T).GetMethod(originalName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly, stub.GetParameters().Select(x => x.ParameterType).Skip(1).ToArray());
            }

            var guard = typeof(ReflectionShims).GetMethod(name, BindingFlags.NonPublic | BindingFlags.Static, stubParameters);

            ArgumentNullException.ThrowIfNull(method);
            ArgumentNullException.ThrowIfNull(stub);
            ArgumentNullException.ThrowIfNull(guard);

            Replace(stub, method);
            Replace(method, guard);
        }
    }

    private uint NextId()
    {
        _idCounter += 1;
        return _idCounter;
    }

    private MethodBuilder CreateGuardMethod(TypeBuilder guardType, MethodBase original, MethodInfo originalStub, CasPermissionSet permissions)
    {
        var result = CreateMethodBuilder(guardType, original);
        var il = result.GetILGenerator();

        il.EmitCall(OpCodes.Call, GuardMethods.GetCallingAssembly, null);
        il.EmitCall(OpCodes.Call, GuardMethods.GetPermissionSet, null);

        foreach (var permission in permissions.Permissions)
        {
            var label = il.DefineLabel();
            il.Emit(OpCodes.Dup);
            il.Emit(OpCodes.Ldc_I8, (long)permission.Id);
            il.EmitCall(OpCodes.Callvirt, GuardMethods.CasPermissionSetHas, null);
            il.Emit(OpCodes.Brtrue_S, label);

            il.Emit(OpCodes.Ldstr, $"Missing required permission {permission.Name} to call method.");
            il.Emit(OpCodes.Newobj, GuardMethods.SecurityExceptionConstructor);
            il.Emit(OpCodes.Throw);

            il.Emit(OpCodes.Nop);
            il.MarkLabel(label);
        }

        il.Emit(OpCodes.Pop);

        var parameterCount = original.GetParameters().Length + (original.IsStatic ? 0 : 1);
        for (var i = 0; i < parameterCount; i++)
        {
            il.Emit(OpCodes.Ldarg, (short)i);
        }
        il.EmitCall(OpCodes.Call, originalStub, null);
        il.Emit(OpCodes.Ret);

        return result;
    }

    private MethodBuilder CreateStubMethod(TypeBuilder guardType, MethodBase original)
    {
        var result = CreateMethodBuilder(guardType, original);
        var il = result.GetILGenerator();
        il.Emit(OpCodes.Ldstr, "Attempted to call stub.");
        il.Emit(OpCodes.Newobj, GuardMethods.SecurityExceptionConstructor);
        il.Emit(OpCodes.Throw);
        return result;
    }

    private MethodBuilder CreateMethodBuilder(TypeBuilder guardType, MethodBase original)
    {
        var attributes = MethodAttributes.Private;

        if (original.IsConstructor || original.IsStatic)
        {
            attributes |= MethodAttributes.Static;
        }

        Type? returnType = null;
        if (original is MethodInfo info)
        {
            returnType = info.ReturnType;
        }

        return guardType.DefineMethod($"{original.Name}_{NextId()}", attributes, returnType, original.GetParameters().Select(x => x.ParameterType).ToArray());
    }

    public static CasPermissionSet GetPermissionSet(Assembly assembly)
    {
        return new CasPermissionSet([]);
    }

    private static void Replace(MethodBase methodToReplace, MethodBase methodToInject)
    {
        RuntimeHelpers.PrepareMethod(methodToReplace.MethodHandle);
        RuntimeHelpers.PrepareMethod(methodToInject.MethodHandle);

        if (methodToReplace.Attributes.HasFlag(MethodAttributes.PinvokeImpl))
        {
            throw new NotSupportedException($"Cannot patch PInvoke method {methodToReplace.Name}");
        }
        if (methodToReplace.IsCollectible)
        {
            throw new NotSupportedException($"Cannot patch collectible method {methodToReplace.Name}");
        }
        if (methodToInject.IsCollectible)
        {
            throw new NotSupportedException($"Cannot patch collectible method {methodToReplace.Name}");
        }

        if (methodToReplace.IsVirtual)
        {
            ReplaceVirtualInner(methodToReplace, methodToInject);
        }
        else
        {
            ReplaceInner(methodToReplace, methodToInject);
        }
    }

    private static void ReplaceVirtualInner(MethodBase methodToReplace, MethodBase methodToInject)
    {
        unsafe
        {
            var methodDesc = (UInt64*)(methodToReplace.MethodHandle.Value.ToPointer());
            int index = (int)(((*methodDesc) >> 32) & 0xFF);
            if (IntPtr.Size == 4)
            {
                var classStart = (uint*)methodToReplace.DeclaringType!.TypeHandle.Value.ToPointer();
                classStart += 10;
                classStart = (uint*)*classStart;
                var tar = classStart + index;

                var inj = (uint*)methodToInject.MethodHandle.Value.ToPointer() + 2;
                *tar = *inj;
            }
            else
            {
                var classStart = (ulong*)methodToReplace.DeclaringType!.TypeHandle.Value.ToPointer();
                classStart += 8;
                classStart = (ulong*)*classStart;
                var tar = classStart + index;

                var inj = (ulong*)methodToInject.MethodHandle.Value.ToPointer() + 1;
                *tar = *inj;
            }
        }
    }

    private static void ReplaceInner(MethodBase methodToReplace, MethodBase methodToInject)
    {
        unsafe
        {
            if (IntPtr.Size == 4)
            {
                var inj = (int*)methodToInject.MethodHandle.Value.ToPointer() + 2;
                var tar = (int*)methodToReplace.MethodHandle.Value.ToPointer() + 2;
                *tar = *inj;
            }
            else
            {
                var inj = (ulong*)methodToInject.MethodHandle.Value.ToPointer() + 1;
                var tar = (ulong*)methodToReplace.MethodHandle.Value.ToPointer() + 1;
                *tar = *inj;
            }
        }
    }

    private static class GuardMethods
    {
        public static ConstructorInfo SecurityExceptionConstructor { get; } = typeof(SecurityException).GetConstructor([typeof(string)])!;
        public static MethodInfo CasPermissionSetHas { get; } = typeof(CasPermissionSet).GetMethod(nameof(CasPermissionSet.Has))!;
        public static MethodInfo GetCallingAssembly { get; } = typeof(Assembly).GetMethod(nameof(Assembly.GetCallingAssembly))!;
        public static MethodInfo GetPermissionSet { get; } = typeof(CasManager).GetMethod(nameof(CasManager.GetPermissionSet))!;
    }

    private class ReflectionShims
    {
        internal static object Invoke(ConstructorInfo info, object?[]? parameters)
        {
            ValidatePermissions(info, Assembly.GetCallingAssembly());
            return ReflectionStubs.Invoke(info, parameters);
        }

        internal static object? Invoke(ConstructorInfo info, object? obj, object?[]? parameters)
        {
            ValidatePermissions(info, Assembly.GetCallingAssembly());
            return ReflectionStubs.Invoke(info, obj, parameters);
        }

        internal static object Invoke(ConstructorInfo info, BindingFlags invokeAttr, Binder? binder, object?[]? parameters, System.Globalization.CultureInfo? culture)
        {
            ValidatePermissions(info, Assembly.GetCallingAssembly());
            return ReflectionStubs.Invoke(info, invokeAttr, binder, parameters, culture);
        }

        internal static object? Invoke(ConstructorInfo info, object? obj, BindingFlags invokeAttr, Binder? binder, object?[]? parameters, System.Globalization.CultureInfo? culture)
        {
            ValidatePermissions(info, Assembly.GetCallingAssembly());
            return ReflectionStubs.Invoke(info, obj, invokeAttr, binder, parameters, culture);
        }

        internal static object? Invoke(MethodInfo info, object? obj, object?[]? parameters)
        {
            ValidatePermissions(info, Assembly.GetCallingAssembly());
            return ReflectionStubs.Invoke(info, obj, parameters);
        }

        internal static object? Invoke(MethodInfo info, object? obj, BindingFlags invokeAttr, Binder? binder, object?[]? parameters, System.Globalization.CultureInfo? culture)
        {
            ValidatePermissions(info, Assembly.GetCallingAssembly());
            return ReflectionStubs.Invoke(info, obj, invokeAttr, binder, parameters, culture);
        }

        private static void ValidatePermissions(MethodBase info, Assembly callingAssembly)
        {
            // todo: protect the shims themselves from being called from reflection

            var permissionSet = GetPermissionSet(callingAssembly);
            if (Instance._currentPolicy._restrictedMethods.TryGetValue(info, out CasPermissionSet? required))
            {
                foreach (var permission in required.Permissions)
                {
                    if (!permissionSet.Has(permission.Id))
                    {
                        throw new SecurityException($"Missing required permission {permission.Name} to call method.");
                    }
                }
            }
        }

        // need to do property get/set
    }

    private class ReflectionStubs
    {
        internal static object Invoke(ConstructorInfo info, object?[]? parameters) => throw new NotImplementedException();
        internal static object? Invoke(ConstructorInfo info, object? obj, object?[]? parameters) => throw new NotImplementedException();
        internal static object Invoke(ConstructorInfo info, BindingFlags invokeAttr, Binder? binder, object?[]? parameters, System.Globalization.CultureInfo? culture) => throw new NotImplementedException();
        internal static object? Invoke(ConstructorInfo info, object? obj, BindingFlags invokeAttr, Binder? binder, object?[]? parameters, System.Globalization.CultureInfo? culture) => throw new NotImplementedException();
        internal static object? Invoke(MethodInfo info, object? obj, object?[]? parameters) => throw new NotImplementedException();
        internal static object? Invoke(MethodInfo info, object? obj, BindingFlags invokeAttr, Binder? binder, object?[]? parameters, System.Globalization.CultureInfo? culture) => throw new NotImplementedException();
    }
}