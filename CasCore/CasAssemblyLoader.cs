using CasCore;
using DouglasDwyer.JitIlVerification;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System.Diagnostics;
using System.Security;

namespace DouglasDwyer.CasCore;

public class CasAssemblyLoader : VerifiableAssemblyLoader
{
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

    protected override void InstrumentAssembly(AssemblyDefinition assembly)
    {
        var methods = GetAllMethods(assembly).ToArray();
        foreach (var method in methods)
        {
            PatchMethod(method.Method, method.References);
        }

        base.InstrumentAssembly(assembly);
    }

    private void PatchMethod(MethodDefinition method, ImportedReferences references)
    {
        if (method.HasBody)
        {
            var rewriter = new MethodBodyRewriter(method);

            while (rewriter.Instruction is not null)
            {
                PatchInstruction(ref rewriter, rewriter.Instruction, references);
                rewriter.Advance();
            }

            rewriter.Finish();
        }
    }

    private void PatchInstruction(ref MethodBodyRewriter rewriter, Instruction instruction, ImportedReferences references)
    {
        // newobj as well!
        if (instruction.OpCode.Code == Code.Call
            || instruction.OpCode.Code == Code.Callvirt
            || instruction.OpCode.Code == Code.Newobj)
        {
            if (((MethodReference)instruction.Operand).Name != ".ctor")
            {
                EmitSecurityException(ref rewriter, references);
            }
        }
    }

    private void PatchMethodCall(ref MethodBodyRewriter rewriter, Instruction instruction, ImportedReferences references)
    {
        var unresolvedTarget = (MethodReference)instruction.Operand;
        MethodDefinition target;
        try
        {
            target = unresolvedTarget.Resolve();
        }
        catch
        {
            // Assembly could not be loaded; type is not one of the ones we have imported
            return;
        }

        var nonVirtual = instruction.OpCode == OpCodes.Call || instruction.OpCode == OpCodes.Newobj
            || (instruction.OpCode == OpCodes.Callvirt && !target.IsVirtual);

        if (nonVirtual) // && method is restricted
        {

        }
        else if (instruction.OpCode == OpCodes.Callvirt)
        {
            // emit can call
        }
    }

    private void EmitSecurityException(ref MethodBodyRewriter rewriter, ImportedReferences references)
    {
        // todo: format exception
        rewriter.Insert(Instruction.Create(OpCodes.Ldstr, "test"));
        rewriter.Insert(Instruction.Create(OpCodes.Newobj, references.SecurityExceptionConstructor));
        rewriter.Insert(Instruction.Create(OpCodes.Throw));
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
            SecurityExceptionConstructor = module.ImportReference(typeof(SecurityException).GetConstructor([typeof(string)]))
        };
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

    /// <summary>
    /// Member references necessary for adding a CAS hook.
    /// </summary>
    private class ImportedReferences
    {
        public required MethodReference SecurityExceptionConstructor;
    }
}