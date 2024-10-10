﻿using CasCore;
using DouglasDwyer.JitIlVerification;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using System.Security;

namespace DouglasDwyer.CasCore;

public class CasAssemblyLoader : VerifiableAssemblyLoader
{
    private static readonly ConditionalWeakTable<Assembly, CasPolicy> _assemblyPolicies = new ConditionalWeakTable<Assembly, CasPolicy>();

    private CasPolicy _policy;

    public CasAssemblyLoader(CasPolicy policy) : base() {
        _policy = policy;
    }

    public CasAssemblyLoader(CasPolicy policy, bool isCollectible) : base(isCollectible)
    {
        _policy = policy;
    }

    public CasAssemblyLoader(CasPolicy policy, string name, bool isCollectible) : base(name, isCollectible)
    {
        _policy = policy;
    }

    public override Assembly LoadFromStream(Stream assembly, Stream? assemblySymbols)
    {
        var result = base.LoadFromStream(assembly, assemblySymbols);
        _assemblyPolicies.Add(result, _policy);
        return result;
    }

    public static bool CanAccess(RuntimeFieldHandle handle)
    {
        return CanAccess(Assembly.GetCallingAssembly(), FieldInfo.GetFieldFromHandle(handle));
    }

    public static bool CanCallAlways(RuntimeMethodHandle handle, RuntimeTypeHandle type)
    {
        var assembly = Assembly.GetCallingAssembly();
        var method = MethodBase.GetMethodFromHandle(handle, type)!;
        if (_assemblyPolicies.TryGetValue(assembly, out CasPolicy? policy))
        {
            var virtualMethod = method.IsVirtual;
            return !virtualMethod && (SameLoadContext(assembly, method) || policy.CanAccess(method));
        }
        else
        {
            return true;
        }
    }

    [StackTraceHidden]
    public static void AssertCanAccess(RuntimeFieldHandle handle, RuntimeTypeHandle type)
    {
        AssertCanAccess(Assembly.GetCallingAssembly(), FieldInfo.GetFieldFromHandle(handle, type));
    }

    [StackTraceHidden]
    public static void AssertCanCall(object? obj, RuntimeMethodHandle handle, RuntimeTypeHandle type)
    {
        AssertCanCall(Assembly.GetCallingAssembly(), obj, MethodBase.GetMethodFromHandle(handle, type)!);
    }

    [StackTraceHidden]
    internal static void ThrowAccessException(Assembly assembly, MemberInfo info)
    {
        throw new SecurityException(FormatSecurityException(assembly, info));
    }

    [StackTraceHidden]
    internal static void AssertCanAccess(Assembly assembly, FieldInfo field)
    {
        if (!CanAccess(assembly, field))
        {
            ThrowAccessException(assembly, field);
        }
    }

    [StackTraceHidden]
    internal static void AssertCanCall(Assembly assembly, object? obj, MethodBase method)
    {
        if (!CanCall(assembly, obj, ref method))
        {
            ThrowAccessException(assembly, method);
        }
    }

    protected override void InstrumentAssembly(AssemblyDefinition assembly)
    {
        var methods = GetAllMethods(assembly).ToArray();
        for (var i = 0; i < methods.Length; i++)
        {
            var method = methods[i];
            PatchMethod(method.Method, i, method.References);
        }

        base.InstrumentAssembly(assembly);
        assembly.Write("otherimpl.dll");
    }

    private static bool CanAccess(Assembly assembly, FieldInfo field)
    {
        if (!SameLoadContext(assembly, field) && _assemblyPolicies.TryGetValue(assembly, out CasPolicy? policy))
        {
            return policy.CanAccess(field);
        }

        return true;
    }

    private static bool CanCall(Assembly assembly, object? obj, ref MethodBase method)
    {
        if (_assemblyPolicies.TryGetValue(assembly, out CasPolicy? policy))
        {
            method = GetTargetMethod(obj, method);
            return SameLoadContext(assembly, method) || policy.CanAccess(method);
        }

        return true;
    }

    private static bool SameLoadContext(Assembly assembly, MemberInfo member)
    {
        return AssemblyLoadContext.GetLoadContext(assembly) == AssemblyLoadContext.GetLoadContext(member.Module.Assembly);
    }

    private void PatchMethod(MethodDefinition method, int id, ImportedReferences references)
    {
        if (method.HasBody)
        {
            var rewriter = new MethodBodyRewriter(method);
            var guardWriter = new GuardWriter(method, id, references);

            while (rewriter.Instruction is not null)
            {
                PatchInstruction(ref rewriter, ref guardWriter, rewriter.Instruction, references);
            }

            rewriter.Finish();
            guardWriter.Finish();
        }
    }

    private void PatchInstruction(ref MethodBodyRewriter rewriter, ref GuardWriter guardWriter, Instruction instruction, ImportedReferences references)
    {
        if (IsMethodOpCode(instruction.OpCode))
        {
            PatchMethodCall(ref rewriter, ref guardWriter, instruction, references);
        }
        else if (IsFieldOpCode(instruction.OpCode))
        {
            PatchFieldAccess(ref rewriter, ref guardWriter, instruction, references);
        }
        else if (instruction.OpCode.Code == Code.Ldftn)
        {
            PatchStaticDelegateCreation(ref rewriter, ref guardWriter, instruction, references);
        }
        else if (instruction.OpCode.Code == Code.Ldvirtftn)
        {
            PatchVirtualDelegateCreation(ref rewriter, ref guardWriter, instruction, references);
        }
        else
        {
            rewriter.Advance(true);
        }
    }

    private bool IsFieldOpCode(OpCode code)
    {
        return code.OperandType == OperandType.InlineField;
    }

    private bool IsMethodOpCode(OpCode code)
    {
        return code.Code == Code.Call || code.Code == Code.Callvirt || code.Code == Code.Newobj;
    }

    private void PatchFieldAccess(ref MethodBodyRewriter rewriter, ref GuardWriter guardWriter, Instruction instruction, ImportedReferences references)
    {
        var target = (FieldReference)instruction.Operand;
        try
        {
            var resolved = target.Resolve();
            if (resolved.Module.Assembly == rewriter.Method.Module.Assembly)
            {
                rewriter.Advance(true);
                return;
            }
        }
        catch
        {
            // If field could not be resolved, then it resides in another assembly, so a check must be inserted
        }

        var accessConstant = guardWriter.GetAccessibilityConstant(target);
        rewriter.Insert(Instruction.Create(OpCodes.Ldsfld, accessConstant));
        var branchTarget = Instruction.Create(OpCodes.Nop);
        rewriter.Insert(Instruction.Create(OpCodes.Brtrue, branchTarget));
        rewriter.Insert(Instruction.Create(OpCodes.Ldtoken, target));
        rewriter.Insert(Instruction.Create(OpCodes.Ldtoken, target.DeclaringType));
        rewriter.Insert(Instruction.Create(OpCodes.Call, references.AssertCanAccess));
        rewriter.Insert(branchTarget);
        rewriter.Advance(true);
    }

    private void PatchStaticDelegateCreation(ref MethodBodyRewriter rewriter, ref GuardWriter guardWriter, Instruction instruction, ImportedReferences references)
    {
        var target = (MethodReference)instruction.Operand;
        try
        {
            var resolved = target.Resolve();
            if (resolved.Module.Assembly == rewriter.Method.Module.Assembly)
            {
                rewriter.Advance(true);
                rewriter.Advance(true);
                return;
            }
        }
        catch
        {
            // If method could not be resolved, then it resides in another assembly, so a check must be inserted
        }

        PatchStaticMethod(ref rewriter, ref guardWriter, target, references);
        rewriter.Advance(true);
        rewriter.Advance(true);
    }

    private void PatchVirtualDelegateCreation(ref MethodBodyRewriter rewriter, ref GuardWriter guardWriter, Instruction instruction, ImportedReferences references)
    {
        var target = (MethodReference)instruction.Operand;
        try
        {
            var resolved = target.Resolve();
            if (resolved.Module.Assembly == rewriter.Method.Module.Assembly)
            {
                rewriter.Advance(true);
                rewriter.Advance(true);
                return;
            }
        }
        catch
        {
            // If method could not be resolved, then it resides in another assembly, so a check must be inserted
        }

        rewriter.Insert(Instruction.Create(OpCodes.Ldtoken, rewriter.Method.Module.ImportReference(target)));
        rewriter.Insert(Instruction.Create(OpCodes.Ldtoken, rewriter.Method.Module.ImportReference(target.DeclaringType)));
        rewriter.Insert(Instruction.Create(OpCodes.Call, references.AssertCanCall));
        rewriter.Insert(Instruction.Create(OpCodes.Dup));

        rewriter.Advance(true);
        rewriter.Advance(true);
    }

    private void PatchMethodCall(ref MethodBodyRewriter rewriter, ref GuardWriter guardWriter, Instruction instruction, ImportedReferences references)
    {
        var target = (MethodReference)instruction.Operand;
        try
        {
            var resolved = target.Resolve();

            if (references.ShimmedMethods.TryGetValue(resolved, out MethodReference? value))
            {
                rewriter.Insert(Instruction.Create(OpCodes.Call, value));
                rewriter.Advance(false);
                return;
            }
            else if (resolved.Module.Assembly == rewriter.Method.Module.Assembly)
            {
                rewriter.Advance(true);
                return;
            }
        }
        catch
        {
            // If method could not be resolved, then it resides in another assembly, so a check must be inserted
        }

        if (instruction.OpCode.Code == Code.Callvirt && target.HasThis)
        {
            PatchVirtualMethod(ref rewriter, ref guardWriter, target, references);
        }
        else
        {
            PatchStaticMethod(ref rewriter, ref guardWriter, target, references);
        }

        rewriter.Advance(true);
    }

    private void PatchVirtualMethod(ref MethodBodyRewriter rewriter, ref GuardWriter guardWriter, MethodReference target, ImportedReferences references)
    {
        rewriter.Method.Body.InitLocals = true;
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
        rewriter.Insert(Instruction.Create(OpCodes.Call, references.AssertCanCall));

        foreach (var local in locals)
        {
            rewriter.Insert(Instruction.Create(OpCodes.Ldloc, local));
        }

        rewriter.Insert(branchTarget);
    }

    private void PatchStaticMethod(ref MethodBodyRewriter rewriter, ref GuardWriter guardWriter, MethodReference target, ImportedReferences references)
    {
        var accessConstant = guardWriter.GetAccessibilityConstant(target);
        rewriter.Insert(Instruction.Create(OpCodes.Ldsfld, accessConstant));
        var branchTarget = Instruction.Create(OpCodes.Nop);
        rewriter.Insert(Instruction.Create(OpCodes.Brtrue, branchTarget));
        rewriter.Insert(Instruction.Create(OpCodes.Ldnull));
        rewriter.Insert(Instruction.Create(OpCodes.Ldtoken, target));
        rewriter.Insert(Instruction.Create(OpCodes.Ldtoken, target.DeclaringType));
        rewriter.Insert(Instruction.Create(OpCodes.Call, references.AssertCanCall));
        rewriter.Insert(branchTarget);
    }

    private List<VariableDefinition> CreateLocalDefinitions(MethodDefinition method, MethodReference target)
    {
        return target.Parameters.Select(x => {
            var parameterType = x.ParameterType;
            if (parameterType is GenericParameter generic)
            {
                if (generic.Owner is MethodReference)
                {
                    var genericMethod = (GenericInstanceMethod)target;
                    parameterType = genericMethod.GenericArguments[generic.Position];
                }
                else
                {
                    var genericType = (GenericInstanceType)target.DeclaringType;
                    parameterType = genericType.GenericArguments[generic.Position];
                }
            }

            return new VariableDefinition(method.Module.ImportReference(parameterType));
        }).ToList();
    }

    /// <summary>
    /// Gets a list of all methods in the assembly, along with imported type references
    /// for their respective modules.
    /// </summary>
    /// <param name="assembly">The assembly definition over which to iterate.</param>
    /// <returns>An enumerable containing methods and imported references.</returns>
    private static IEnumerable<MethodToUpdate> GetAllMethods(AssemblyDefinition assembly)
    {
        return assembly.Modules
            .Select(x => (x, ImportReferences(x)))
            .SelectMany(x => x.x.Types.Select(y => (y, x.Item2)))
            .SelectMany(x => GetAllMethods(x.y).Select(y => new MethodToUpdate
            {
                Method = y,
                References = x.Item2
            }))
            .Where(x => x.Method.HasBody);
    }

    /// <summary>
    /// Gets all methods associated with the given type (including methods in nested types).
    /// </summary>
    /// <param name="type">The type over which to iterate.</param>
    /// <returns>All methods contained in the type.</returns>
    private static IEnumerable<MethodDefinition> GetAllMethods(TypeDefinition type)
    {
        return type.Methods
            .Concat(type.NestedTypes.SelectMany(GetAllMethods));
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
            AssertCanAccess = module.ImportReference(typeof(CasAssemblyLoader).GetMethod(nameof(AssertCanAccess))),
            AssertCanCall = module.ImportReference(typeof(CasAssemblyLoader).GetMethod(nameof(AssertCanCall))),
            BoolType = module.ImportReference(typeof(bool)),
            CanAccess = module.ImportReference(typeof(CasAssemblyLoader).GetMethod(nameof(CanAccess))),
            CanCallAlways = module.ImportReference(typeof(CasAssemblyLoader).GetMethod(nameof(CanCallAlways))),
            ObjectType = module.ImportReference(typeof(object)),
            VoidType = module.ImportReference(typeof(void)),
        };
    }

    private static IImmutableDictionary<MethodDefinition, MethodReference> ImportShims(ModuleDefinition module)
    {
        return MethodShims.ShimMap.ToImmutableDictionary(x => module.ImportReference(x.Key).Resolve(), x => module.ImportReference(x.Value));
    }

    private static string FormatSecurityException(AssemblyDefinition assembly, MemberInfo member)
    {
        return FormatSecurityException(assembly.Name.Name, member);
    }

    private static string FormatSecurityException(Assembly assembly, MemberInfo member)
    {
        return FormatSecurityException(assembly.GetName().Name, member);
    }

    private static MethodBase GetTargetMethod(object? obj, MethodBase method)
    {
        if (method is MethodInfo info && method.IsVirtual)
        {
            var objType = obj!.GetType();
            MethodInfo? targetMethod = null;
            if (info.DeclaringType!.IsInterface)
            {
                var map = objType.GetInterfaceMap(info.DeclaringType);
                var index = Array.IndexOf(map.InterfaceMethods, info);
                targetMethod = map.TargetMethods[index];
            }
            else
            {
                targetMethod = objType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                    .FirstOrDefault(x => x.GetBaseDefinition() == info.GetBaseDefinition());
            }

            ArgumentNullException.ThrowIfNull(targetMethod);
            return targetMethod;
        }
        else
        {
            return method;
        }
    }

    private static string FormatSecurityException(string? assemblyName, MemberInfo member)
    {
        if (assemblyName is null)
        {
            return $"Assembly does not have permission to access {member} of {member.DeclaringType}.";
        }
        else
        {
            return $"Assembly {assemblyName} does not have permission to access {member} of {member.DeclaringType}.";
        }
    }

    /// <summary>
    /// Data necessary for adding a CAS hook to a method.
    /// </summary>
    private struct MethodToUpdate
    {
        /// <summary>
        /// The method that needs CAS hooks.
        /// </summary>
        public required MethodDefinition Method;

        /// <summary>
        /// Member references necessary for adding a CAS hook.
        /// </summary>
        public required ImportedReferences References;
    }
}