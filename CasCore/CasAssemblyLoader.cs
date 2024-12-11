using CasCore;
using DouglasDwyer.JitIlVerification;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using System.Security;

namespace DouglasDwyer.CasCore;

/// <summary>
/// Provides scoped assembly loading with the same semantics as <see cref="AssemblyLoadContext"/>.
/// Any assemblies loaded with this context will be subject to its <see cref="CasPolicy"/>,
/// and any attempts to access unwhitelisted external fields/methods will throw exceptions.
/// </summary>
public class CasAssemblyLoader : VerifiableAssemblyLoader
{
    /// <summary>
    /// A mapping from assembly to its associated loader.
    /// </summary>
    private static readonly ConditionalWeakTable<Assembly, CasAssemblyLoader> _assemblyLoaders = new ConditionalWeakTable<Assembly, CasAssemblyLoader>();

    /// <summary>
    /// A function for shallow-cloning objects.
    /// </summary>
    private static Func<object, object> MemberwiseCloneFunc { get; } = (Func<object, object>)Delegate.CreateDelegate(
        typeof(Func<object, object>), typeof(object).GetMethod("MemberwiseClone", BindingFlags.NonPublic | BindingFlags.Instance)!);
    
    /// <summary>
    /// Facilitates setting the list of generic instance arguments on a <see cref="GenericInstanceType"/>.
    /// </summary>
    private static FieldInfo GenericInstanceTypeArguments { get; } = typeof(GenericInstanceType).GetField("arguments", BindingFlags.NonPublic | BindingFlags.Instance)!;

    /// <summary>
    /// The handler that will be invoked whenever a sandboxed assembly accesses a field/method without permission.
    /// By default, this is a <see cref="ExceptionViolationHandler"/> that will throw an exception.
    /// </summary>
    public ICasViolationHandler ViolationHandler { get; set; } = new ExceptionViolationHandler();

    /// <summary>
    /// The policy that will apply to any assemblies created with this loader.
    /// </summary>
    private readonly CasPolicy _policy;

    /// <summary>
    /// Creates a new loader with the given policy.
    /// </summary>
    /// <param name="policy">The policy that will apply to any assemblies created with this loader.</param>
    public CasAssemblyLoader(CasPolicy policy) : base() {
        _policy = policy;
    }

    /// <summary>
    /// Creates a new loader with the given policy.
    /// </summary>
    /// <param name="policy">The policy that will apply to any assemblies created with this loader.</param>
    /// <param name="isCollectible">Whether this context should be able to unload.</param>
    public CasAssemblyLoader(CasPolicy policy, bool isCollectible) : base(isCollectible)
    {
        _policy = policy;
    }

    /// <summary>
    /// Creates a new loader with the given policy.
    /// </summary>
    /// <param name="policy">The policy that will apply to any assemblies created with this loader.</param>
    /// <param name="name">The display name of the load context.</param>
    /// <param name="isCollectible">Whether this context should be able to unload.</param>
    public CasAssemblyLoader(CasPolicy policy, string name, bool isCollectible) : base(name, isCollectible)
    {
        _policy = policy;
    }

    /// <inheritdoc/>
    public override Assembly LoadFromStream(Stream assembly, Stream? assemblySymbols)
    {
        var result = base.LoadFromStream(assembly, assemblySymbols);
        _assemblyLoaders.Add(result, this);
        return result;
    }

    /// <summary>
    /// Invokes the calling assembly's CAS violation handler for the provided method.
    /// </summary>
    /// <param name="handle">The field handle.</param>
    /// <param name="type">The type handle on which the field is declared.</param>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [StackTraceHidden]
    public static void InvokeViolationHandler(RuntimeMethodHandle handle, RuntimeTypeHandle type)
    {
        HandleCasViolation(Assembly.GetCallingAssembly(), MethodBase.GetMethodFromHandle(handle, type)!);
    }

    /// <summary>
    /// Determines whether the calling assembly may access the specified field.
    /// </summary>
    /// <param name="handle">The field handle.</param>
    /// <param name="type">The type handle on which the field is declared.</param>
    /// <returns>Whether the field is accessible.</returns>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static bool CanAccess(RuntimeFieldHandle handle, RuntimeTypeHandle type)
    {
        return CanAccess(Assembly.GetCallingAssembly(), FieldInfo.GetFieldFromHandle(handle, type));
    }

    /// <summary>
    /// Determines whether the calling assembly may always access the specified method.
    /// </summary>
    /// <param name="handle">The method handle.</param>
    /// <param name="type">The type handle on which the method is declared.</param>
    /// <returns>Whether the method is always callable.</returns>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static bool CanCallAlways(RuntimeMethodHandle handle, RuntimeTypeHandle type)
    {
        return CanCallAlways(Assembly.GetCallingAssembly(), MethodBase.GetMethodFromHandle(handle, type)!);
    }

    /// <summary>
    /// Checks if the calling assembly may access the specified field.
    /// If not, invokes the CAS violation handler.
    /// </summary>
    /// <param name="handle">The field handle.</param>
    /// <param name="type">The type handle on which the field is declared.</param>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [StackTraceHidden]
    public static void CheckAccess(RuntimeFieldHandle handle, RuntimeTypeHandle type)
    {
        CheckAccess(Assembly.GetCallingAssembly(), FieldInfo.GetFieldFromHandle(handle, type));
    }

    /// <summary>
    /// Checks if the calling assembly may call the specified method with a <c>callvirt</c> instruction.
    /// If not, invokes the CAS violation handler.
    /// </summary>
    /// <param name="obj">The object on which the method is being invoked, if any.</param>
    /// <param name="handle">The method handle.</param>
    /// <param name="type">The type handle on which the method is declared.</param>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [StackTraceHidden]
    public static void CheckVirtualCall(object? obj, RuntimeMethodHandle handle, RuntimeTypeHandle type)
    {
        CheckVirtualCall(Assembly.GetCallingAssembly(), obj, MethodBase.GetMethodFromHandle(handle, type)!);
    }

    /// <summary>
    /// Checks if the calling assembly may call the specified method with a constrained <c>callvirt</c> instruction.
    /// If not, invokes the CAS violation handler.
    /// </summary>
    /// <typeparam name="T">The type on which the method is being invoked.</typeparam>
    /// <param name="obj">The object on which the method is being invoked, if any.</param>
    /// <param name="handle">The method handle.</param>
    /// <param name="type">The type handle on which the method is declared.</param>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [StackTraceHidden]
    public static void CheckVirtualCallConstrained<T>(ref T obj, RuntimeMethodHandle handle, RuntimeTypeHandle type)
    {
        CheckVirtualCall(Assembly.GetCallingAssembly(), obj, MethodBase.GetMethodFromHandle(handle, type)!);
    }

    /// <summary>
    /// Creates a delegate, but throws an exception when the calling assembly does not have the requisite permissions.
    /// </summary>
    /// <typeparam name="T">The type of delegate to create.</typeparam>
    /// <param name="target">The object to which the delegate method should be bound.</param>
    /// <param name="method">The method handle.</param>
    /// <param name="type">The type handle on which the method is declared.</param>
    /// <returns>The delegate that was created.</returns>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [StackTraceHidden]
    public static T CreateCheckedDelegate<T>(object? target, RuntimeMethodHandle method, RuntimeTypeHandle type)
        where T : Delegate
    {
        var originalMethod = (MethodInfo)MethodBase.GetMethodFromHandle(method, type)!;
        var targetMethod = originalMethod;

        if (MethodShims.TryGetShim(targetMethod, out MethodInfo? shim))
        {
            targetMethod = shim;
        }

        Delegate result;
        if (originalMethod.IsStatic)
        {
            result = Delegate.CreateDelegate(typeof(T), targetMethod);
        }
        else
        {
            result = Delegate.CreateDelegate(typeof(T), target, targetMethod);
        }

        if (originalMethod == targetMethod)
        {
            CheckVirtualCall(Assembly.GetCallingAssembly(), result.Target, result.Method);
        }

        return (T)result;
    }

    /// <summary>
    /// Throws a security exception that specifies the assembly does not have permission to access the member.
    /// </summary>
    /// <param name="assembly">The assembly that tried to access the member.</param>
    /// <param name="info">The member being accessed.</param>
    /// <exception cref="InvalidOperationException">
    /// If the assembly did not have an associated CAS loader.
    /// </exception>
    [StackTraceHidden]
    internal static void HandleCasViolation(Assembly assembly, MemberInfo info)
    {
        if (_assemblyLoaders.TryGetValue(assembly, out CasAssemblyLoader? loader))
        {
            loader.ViolationHandler.OnViolation(assembly, info);
        }
        else
        {
            throw new InvalidOperationException($"Sandboxed loader for {assembly} did not exist");
        }
    }

    /// <summary>
    /// Throws an exception if the given assembly may not access the specified field.
    /// </summary>
    /// <param name="assembly">The assembly attempting the access.</param>
    /// <param name="field">The field being accessed.</param>
    [StackTraceHidden]
    internal static void CheckAccess(Assembly assembly, FieldInfo field)
    {
        if (!CanAccess(assembly, field))
        {
            HandleCasViolation(assembly, field);
        }
    }

    /// <summary>
    /// Throws an exception if the calling assembly may not call the specified method.
    /// </summary>
    /// <param name="assembly">The assembly attempting the access.</param>
    /// <param name="obj">The object on which the method is being invoked, if any.</param>
    /// <param name="method">The method being called.</param>
    [StackTraceHidden]
    internal static void CheckVirtualCall(Assembly assembly, object? obj, MethodBase method)
    {
        if (!CanCall(assembly, obj, ref method))
        {
            HandleCasViolation(assembly, method);
        }
    }

    /// <summary>
    /// Determines whether the given assembly may always access the specified method.
    /// </summary>
    /// <param name="assembly">The assembly attempting the access.</param>
    /// <param name="method">The method in question.</param>
    /// <returns>Whether the method is always callable.</returns>
    internal static bool CanCallAlways(Assembly assembly, MethodBase method)
    {
        if (_assemblyLoaders.TryGetValue(assembly, out CasAssemblyLoader? loader))
        {
            var virtualMethod = method.IsVirtual && !method.IsFinal;
            return SameAssemblyLoader(loader, method) || (!virtualMethod && loader._policy.CanAccess(method));
        }
        else
        {
            return true;
        }
    }

    /// <inheritdoc/>
    protected override Assembly? Load(AssemblyName assemblyName)
    {
        Assembly executingAssembly = Assembly.GetExecutingAssembly();
        if (AssemblyName.ReferenceMatchesDefinition(assemblyName, executingAssembly.GetName()))
        {
            return executingAssembly;
        }

        return base.Load(assemblyName);
    }

    /// <inheritdoc/>
    protected override nint LoadUnmanagedDll(string unmanagedDllName)
    {
        throw new SecurityException("CAS assemblies may not load unmanaged libraries.");
    }

    /// <inheritdoc/>
    protected override void InstrumentAssembly(AssemblyDefinition assembly)
    {
        base.InstrumentAssembly(assembly);

        var id = 0;
        foreach (var module in assembly.Modules)
        {
            var references = ImportReferences(module);
            var rewriter = new MethodBodyRewriter(references);

            foreach (var type in GetAllTypes(module).Where(x => 0 < x.Methods.Count).ToArray())
            {
                var guardWriter = new GuardWriter(type, id, references);
                
                foreach (var method in type.Methods.Where(x => x.HasBody))
                {
                    PatchMethod(method, rewriter, guardWriter, references);
                }

                guardWriter.Finish();
                id++;
            }
        }
    }

    /// <summary>
    /// Determines whether the given assembly may access the specified field.
    /// </summary>
    /// <param name="assembly">The assembly attempting the access.</param>
    /// <param name="field">The field being accessed.</param>
    /// <returns>Whether the assembly has permission to access the field.</returns>
    /// <exception cref="InvalidOperationException">
    /// If no policy was associated with the given assembly.
    /// </exception>
    private static bool CanAccess(Assembly assembly, FieldInfo field)
    {
        if (_assemblyLoaders.TryGetValue(assembly, out CasAssemblyLoader? loader))
        {
            return SameAssemblyLoader(loader, field) || loader._policy.CanAccess(field);
        }
        else
        {
            throw new InvalidOperationException($"No policy set for assembly {assembly}.");
        }
    }

    /// <summary>
    /// Determines whether the given assembly may access the specified method.
    /// </summary>
    /// <param name="assembly">The assembly attempting the access.</param>
    /// <param name="obj">The object on which the method is being invoked, if any.</param>
    /// <param name="method">The method being accessed.</param>
    /// <returns>Whether the assembly has permission to access the method.</returns>
    /// <exception cref="InvalidOperationException">
    /// If no policy was associated with the given assembly.
    /// </exception>
    private static bool CanCall(Assembly assembly, object? obj, ref MethodBase method)
    {
        if (_assemblyLoaders.TryGetValue(assembly, out CasAssemblyLoader? loader))
        {
            method = LateBindingResolver.GetTargetMethod(obj, method);
            return SameAssemblyLoader(loader, method) || loader._policy.CanAccess(method);
        }
        else
        {
            throw new InvalidOperationException($"No policy set for assembly {assembly}.");
        }
    }

    /// <summary>
    /// Determines if the provided member exists within the same <see cref="CasAssemblyLoader"/>.
    /// </summary>
    /// <param name="loader">The loader against which to compare.</param>
    /// <param name="member">The member.</param>
    /// <returns>Whether the member and assembly share a load context.</returns>
    private static bool SameAssemblyLoader(CasAssemblyLoader loader, MemberInfo member)
    {
        if (_assemblyLoaders.TryGetValue(member.Module.Assembly, out CasAssemblyLoader? memberLoader))
        {
            return loader == memberLoader;
        }
        else
        {
            return false;
        }
    }

    /// <summary>
    /// Rewrites the body of a method to include runtime checks for code access security.
    /// </summary>
    /// <param name="method">The method to instrument.</param>
    /// <param name="rewriter">The rewriter to use for instrumenting the method.</param>
    /// <param name="guardWriter">The object to use for generating guard access fields.</param>
    /// <param name="references">The external references.</param>
    private void PatchMethod(MethodDefinition method, MethodBodyRewriter rewriter, GuardWriter guardWriter, ImportedReferences references)
    {
        if (method.HasBody && HasJitVerificationGuard(method))
        {
            rewriter.Start(method);

            // Advance past JIT guard
            rewriter.Advance(true);
            rewriter.Advance(true);

            while (rewriter.Instruction is not null)
            {
                PatchInstruction(rewriter, guardWriter, references);
            }

            rewriter.Finish();
        }
    }

    /// <summary>
    /// Rewrites a single method instruction to include runtime checks for code access security.
    /// </summary>
    /// <param name="rewriter">The rewriter to use for instrumenting the method.</param>
    /// <param name="guardWriter">The object to use for generating guard access fields.</param>
    /// <param name="references">The external references.</param>
    private void PatchInstruction(MethodBodyRewriter rewriter, GuardWriter guardWriter, ImportedReferences references)
    {
        if (IsMethodOpCode(rewriter.Instruction!.OpCode))
        {
            PatchMethodCall(rewriter, guardWriter, references);
        }
        else if (IsFieldOpCode(rewriter.Instruction.OpCode))
        {
            PatchFieldAccess(rewriter, guardWriter, references);
        }
        else if (rewriter.Instruction.OpCode.Code == Code.Ldftn
            || rewriter.Instruction.OpCode.Code == Code.Ldvirtftn)
        {
            PatchDelegateCreation(rewriter, guardWriter, references);
        }
        else
        {
            rewriter.Advance(true);
        }
    }

    /// <summary>
    /// Determines whether the provided opcode involves a field access.
    /// </summary>
    /// <param name="code">The opcode in question.</param>
    /// <returns>Whether a field access needs to be patched for this operation.</returns>
    private bool IsFieldOpCode(OpCode code)
    {
        return code.OperandType == OperandType.InlineField;
    }

    /// <summary>
    /// Determines whether the provided opcode involves a method call.
    /// </summary>
    /// <param name="code">The opcode in question.</param>
    /// <returns>Whether a method call needs to be patched for this operation.</returns>
    private bool IsMethodOpCode(OpCode code)
    {
        return code.Code == Code.Call || code.Code == Code.Callvirt || code.Code == Code.Newobj;
    }

    /// <summary>
    /// Inserts a runtime access check before a field access.
    /// </summary>
    /// <param name="rewriter">The method instrumentor.</param>
    /// <param name="guardWriter">The object to use for generating guard access fields.</param>
    /// <param name="references">The external references.</param>
    private void PatchFieldAccess(MethodBodyRewriter rewriter, GuardWriter guardWriter, ImportedReferences references)
    {
        var target = (FieldReference)rewriter.Instruction!.Operand;

        if (rewriter.Method.DeclaringType.Scope == target.DeclaringType.Scope)
        {
            rewriter.Advance(true);
            return;
        }

        var accessConstant = guardWriter.GetAccessibilityConstant(target);
        rewriter.Insert(Instruction.Create(OpCodes.Ldsfld, accessConstant));
        var branchTarget = Instruction.Create(OpCodes.Nop);
        rewriter.Insert(Instruction.Create(OpCodes.Brtrue, branchTarget));
        rewriter.Insert(Instruction.Create(OpCodes.Ldtoken, target));
        rewriter.Insert(Instruction.Create(OpCodes.Ldtoken, target.DeclaringType));
        rewriter.Insert(Instruction.Create(OpCodes.Call, references.CheckAccess));
        rewriter.Insert(branchTarget);
        rewriter.Advance(true);
    }

    /// <summary>
    /// Replaces a delegate creation expression with a shim for runtime checking.
    /// </summary>
    /// <param name="rewriter">The method instrumentor.</param>
    /// <param name="guardWriter">The object to use for generating guard access fields.</param>
    /// <param name="references">The external references.</param>
    private void PatchDelegateCreation(MethodBodyRewriter rewriter, GuardWriter guardWriter, ImportedReferences references)
    {
        var target = (MethodReference)rewriter.Instruction!.Operand;
        if (rewriter.Method.DeclaringType.Scope == target.DeclaringType.Scope)
        {
            rewriter.Advance(true);
            rewriter.Advance(true);
            return;
        }

        var targetDelegate = ((MethodReference)rewriter.Instruction.Next.Operand).DeclaringType;
        var createChecked = new GenericInstanceMethod(references.CreateCheckedDelegate);
        createChecked.GenericArguments.Add(targetDelegate);

        if (rewriter.Instruction.OpCode.Code == Code.Ldvirtftn)
        {
            rewriter.Insert(Instruction.Create(OpCodes.Pop));
        }

        rewriter.Insert(Instruction.Create(OpCodes.Ldtoken, rewriter.Method.Module.ImportReference(target)));
        rewriter.Insert(Instruction.Create(OpCodes.Ldtoken, rewriter.Method.Module.ImportReference(target.DeclaringType)));
        rewriter.Insert(Instruction.Create(OpCodes.Call, createChecked));

        rewriter.Advance(false);
        rewriter.Advance(false);
    }

    /// <summary>
    /// Inserts a runtime access check before a method call, or replaces the method call with a shim if necessary.
    /// </summary>
    /// <param name="rewriter">The method instrumentor.</param>
    /// <param name="guardWriter">The object to use for generating guard access fields.</param>
    /// <param name="references">The external references.</param>
    private void PatchMethodCall(MethodBodyRewriter rewriter, GuardWriter guardWriter, ImportedReferences references)
    {
        var target = (MethodReference)rewriter.Instruction!.Operand;
        if (rewriter.Method.DeclaringType.Scope == target.DeclaringType.Scope)
        {
            rewriter.Advance(true);
            return;
        }
        else
        {
            if (references.ShimmedMethods.TryGetValue(new SignatureHash(target), out MethodReference? value))
            {
                if (target.DeclaringType is GenericInstanceType
                    || target is GenericInstanceMethod)
                {
                    var newValue = new GenericInstanceMethod(value);
                    if (target.DeclaringType is GenericInstanceType git)
                    {
                        foreach (var arg in git.GenericArguments)
                        {
                            newValue.GenericArguments.Add(arg);
                        }
                    }

                    if (target is GenericInstanceMethod gim)
                    {
                        foreach (var arg in gim.GenericArguments)
                        {
                            newValue.GenericArguments.Add(arg);
                        }
                    }

                    value = newValue;
                }

                rewriter.Insert(Instruction.Create(OpCodes.Call, value));
                rewriter.Advance(false);
                return;
            }
        }

        if (rewriter.Instruction.OpCode.Code == Code.Callvirt && target.HasThis)
        {
            PatchVirtualMethod(rewriter, guardWriter, target, references);
        }
        else
        {
            PatchStaticMethod(rewriter, guardWriter, target, references);
        }

        rewriter.Advance(true);
    }

    /// <summary>
    /// Inserts a runtime access check before a virtual method call.
    /// </summary>
    /// <param name="rewriter">The method instrumentor.</param>
    /// <param name="guardWriter">The object to use for generating guard access fields.</param>
    /// <param name="target">The target method being called.</param>
    /// <param name="references">The external references.</param>
    private void PatchVirtualMethod(MethodBodyRewriter rewriter, GuardWriter guardWriter, MethodReference target, ImportedReferences references)
    {
        rewriter.Method.Body.InitLocals = true;

        var isConstrained = rewriter.Instruction!.Previous is not null && rewriter.Instruction.Previous.OpCode.Code == Code.Constrained;

        var accessConstant = guardWriter.GetAccessibilityConstant(target);
        rewriter.Insert(Instruction.Create(OpCodes.Ldsfld, accessConstant));
        var branchTarget = Instruction.Create(OpCodes.Nop);
        rewriter.Insert(Instruction.Create(OpCodes.Brtrue, branchTarget));

        var locals = CreateLocalDefinitions(rewriter.Method, target);
        foreach (var local in ((IEnumerable<VariableDefinition>)locals).Reverse())
        {
            rewriter.Method.Body.Variables.Add(local);
            rewriter.Insert(Instruction.Create(OpCodes.Stloc, local));
        }

        rewriter.Insert(Instruction.Create(OpCodes.Dup));
        rewriter.Insert(Instruction.Create(OpCodes.Ldtoken, rewriter.Method.Module.ImportReference(target)));
        rewriter.Insert(Instruction.Create(OpCodes.Ldtoken, rewriter.Method.Module.ImportReference(target.DeclaringType)));

        if (isConstrained)
        {
            var genericAssert = new GenericInstanceMethod(references.CheckVirtualCallConstrained);
            genericAssert.GenericArguments.Add((TypeReference)rewriter.Instruction.Previous!.Operand);
            rewriter.Insert(Instruction.Create(OpCodes.Call, genericAssert));
        }
        else
        {
            rewriter.Insert(Instruction.Create(OpCodes.Call, references.CheckVirtualCall));
        }

        foreach (var local in locals)
        {
            rewriter.Insert(Instruction.Create(OpCodes.Ldloc, local));
        }

        rewriter.Insert(branchTarget);
    }

    /// <summary>
    /// Inserts a runtime access check before a static method call.
    /// </summary>
    /// <param name="rewriter">The method instrumentor.</param>
    /// <param name="guardWriter">The object to use for generating guard access fields.</param>
    /// <param name="target">The target method being called.</param>
    /// <param name="references">The external references.</param>
    private void PatchStaticMethod(MethodBodyRewriter rewriter, GuardWriter guardWriter, MethodReference target, ImportedReferences references)
    {
        var accessConstant = guardWriter.GetAccessibilityConstant(target);
        rewriter.Insert(Instruction.Create(OpCodes.Ldsfld, accessConstant));
        var branchTarget = Instruction.Create(OpCodes.Nop);
        rewriter.Insert(Instruction.Create(OpCodes.Brtrue, branchTarget));
        rewriter.Insert(Instruction.Create(OpCodes.Ldtoken, target));
        rewriter.Insert(Instruction.Create(OpCodes.Ldtoken, target.DeclaringType));
        rewriter.Insert(Instruction.Create(OpCodes.Call, references.InvokeViolationHandler));
        rewriter.Insert(branchTarget);
    }

    /// <summary>
    /// Creates a list of local variables - one for each parameter of the given target method.
    /// </summary>
    /// <param name="method">The parent method.</param>
    /// <param name="target">The method being called.</param>
    /// <returns>A list of local variables, with the same types as <paramref name="target"/>'s parameters.</returns>
    private List<VariableDefinition> CreateLocalDefinitions(MethodDefinition method, MethodReference target)
    {
        return target.Parameters.Select(x => new VariableDefinition(method.Module.ImportReference(ResolveGenericParameter(x.ParameterType, target)))).ToList();
    }

    /// <summary>
    /// Determines the concrete type that should be associated with a generic parameter in a method invocation.
    /// </summary>
    /// <param name="type">The generic-qualified type to replace with a concrete instance.</param>
    /// <param name="target">The target method being called.</param>
    /// <returns>The type, with any generic parameters from the target method removed.</returns>
    /// <exception cref="NotSupportedException">If the kind of type to resolve was unrecognized.</exception>
    private static TypeReference ResolveGenericParameter(TypeReference type, MethodReference target)
    {
        if (!type.ContainsGenericParameter)
        {
            return type;
        }

        switch(type)
        {
            case GenericParameter genericParam:
                if (genericParam.Owner is MethodReference)
                {
                    var genericMethod = (GenericInstanceMethod)target;
                    return genericMethod.GenericArguments[genericParam.Position];
                }
                else
                {
                    var genericType = (GenericInstanceType)target.DeclaringType;
                    return genericType.GenericArguments[genericParam.Position];
                }
            case ArrayType array:
                return ResolveGenericParameter(array.ElementType, target).MakeArrayType();
            case GenericInstanceType inst:
                var newInst = (GenericInstanceType)MemberwiseCloneFunc(inst);
                var newArguments = new Mono.Collections.Generic.Collection<TypeReference>(inst.GenericArguments.Count);
                foreach (var arg in inst.GenericArguments)
                {
                    newArguments.Add(ResolveGenericParameter(arg, target));
                }
                GenericInstanceTypeArguments.SetValue(newInst, newArguments);
                return newInst;
            case ByReferenceType byReference:
                return ResolveGenericParameter(byReference.ElementType, target).MakeByReferenceType();
            default:
                throw new NotSupportedException($"Unable to resolve generic parameter {type} for {target}");
        }
    }

    /// <summary>
    /// Gets all types associated with the given module.
    /// </summary>
    /// <param name="module">The module over which to iterate.</param>
    /// <returns>All types contained in the module, including nested types.</returns>
    private static IEnumerable<TypeDefinition> GetAllTypes(ModuleDefinition module)
    {
        return module.Types.SelectMany(GetAllTypes);
    }

    /// <summary>
    /// Gets all types associated with the given type (including both the original type and its nested types).
    /// </summary>
    /// <param name="type">The type over which to iterate.</param>
    /// <returns>All types in this type's tree.</returns>
    private static IEnumerable<TypeDefinition> GetAllTypes(TypeDefinition type)
    {
        return type.NestedTypes.SelectMany(GetAllTypes).Append(type);
    }

    /// <summary>
    /// Adds type references to the given module that are necessary for guard type implementations.
    /// </summary>
    /// <param name="module">The module where the types should be imported.</param>
    /// <returns>A set of type references that were imported.</returns>
    private static ImportedReferences ImportReferences(ModuleDefinition module)
    {
        return new ImportedReferences
        {
            ShimmedMethods = ImportShims(module),
            CheckAccess = module.ImportReference(typeof(CasAssemblyLoader).GetMethod(nameof(CheckAccess))),
            CheckVirtualCall = module.ImportReference(typeof(CasAssemblyLoader).GetMethod(nameof(CheckVirtualCall))),
            CheckVirtualCallConstrained = module.ImportReference(typeof(CasAssemblyLoader).GetMethod(nameof(CheckVirtualCallConstrained))),
            CreateCheckedDelegate = module.ImportReference(typeof(CasAssemblyLoader).GetMethod(nameof(CreateCheckedDelegate))),
            InvokeViolationHandler = module.ImportReference(typeof(CasAssemblyLoader).GetMethod(nameof(InvokeViolationHandler))),
            BoolType = module.ImportReference(typeof(bool)),
            CanAccess = module.ImportReference(typeof(CasAssemblyLoader).GetMethod(nameof(CanAccess))),
            CanCallAlways = module.ImportReference(typeof(CasAssemblyLoader).GetMethod(nameof(CanCallAlways))),
            ObjectType = module.ImportReference(typeof(object)),
            VoidType = module.ImportReference(typeof(void)),
        };
    }

    /// <summary>
    /// Imports references to all shim methods.
    /// </summary>
    /// <param name="module">The module on which to import the shims.</param>
    /// <returns>A map from original method signature to shim method.</returns>
    private static IImmutableDictionary<SignatureHash, MethodReference> ImportShims(ModuleDefinition module)
    {
        return MethodShims.ShimMap.ToImmutableDictionary(x => x.Key, x => module.ImportReference(x.Value));
    }

    /// <summary>
    /// Determines whether the given method was a part of the original assembly
    /// (as opposed to being an added method for JIT IL verification).
    /// </summary>
    /// <param name="method">The method to check.</param>
    /// <returns>
    /// Whether the method has a JIT verification guard, indicating that it
    /// is a method from the original assembly.
    /// </returns>
    private static bool HasJitVerificationGuard(MethodDefinition method)
    {
        return 2 <= method.Body.Instructions.Count
            && method.Body.Instructions[0].OpCode.Code == Code.Ldsfld
            && method.Body.Instructions[1].OpCode.Code == Code.Pop;
    }
}