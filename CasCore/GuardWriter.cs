using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Collections.Generic;

namespace DouglasDwyer.CasCore;

/// <summary>
/// Creates a series of static fields that indicate what an assembly may/may not access.
/// </summary>
internal class GuardWriter
{
    /// <summary>
    /// The type containing the methods that this writer will be guarding.
    /// </summary>
    public TypeDefinition DeclaringType { get; }

    /// <summary>
    /// The static constructor that will initialize all guard fields.
    /// </summary>
    private readonly MethodDefinition _staticConstructor;

    /// <summary>
    /// The object to which IL should be written for initializing guard fields.
    /// </summary>
    private readonly ILProcessor _il;

    /// <summary>
    /// The type containing the static constructor and guard field.
    /// </summary>
    private readonly TypeDefinition _type;

    /// <summary>
    /// References to use when initializing guard fields.
    /// </summary>
    private readonly ImportedReferences _references;

    /// <summary>
    /// A map from fields to their generated guard flags.
    /// </summary>
    private readonly Dictionary<FieldReference, FieldReference> _fieldGuards;

    /// <summary>
    /// A map from methods to their generated guard flags.
    /// </summary>
    private readonly Dictionary<MethodReference, FieldReference> _methodGuards;

    /// <summary>
    /// The number of guards that have been generated.
    /// </summary>
    private int _fieldCount;

    /// <summary>
    /// Creates a new writer for generating guard flags associated with method calls
    /// from methods in the given type.
    /// </summary>
    /// <param name="type">The type that needs runtime checks.</param>
    /// <param name="id">A unique ID to use for this guard.</param>
    /// <param name="references">The imported references to use for runtime checks.</param>
    public GuardWriter(TypeDefinition type, int id, ImportedReferences references)
    {
        DeclaringType = type;
        _type = new TypeDefinition("CasCore.Guard", $"{DeclaringType.Name}_{id}",
            TypeAttributes.Public | TypeAttributes.Class | TypeAttributes.AutoLayout
            | TypeAttributes.Abstract | TypeAttributes.Sealed, references.ObjectType);

        _staticConstructor = new MethodDefinition(".cctor", MethodAttributes.Public | MethodAttributes.HideBySig
            | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName | MethodAttributes.Static, references.VoidType);
        _type.Methods.Add(_staticConstructor);

        _il = _staticConstructor.Body.GetILProcessor();
        _fieldCount = 0;
        _references = references;
        _fieldGuards = new Dictionary<FieldReference, FieldReference>();
        _methodGuards = new Dictionary<MethodReference, FieldReference>();
    }

    /// <summary>
    /// Completes writing guard fields and adds the guard type to the module.
    /// </summary>
    public void Finish()
    {
        if (0 < _fieldCount)
        {
            _il.Append(_il.Create(OpCodes.Ret));
            DeclaringType.Module.Types.Add(_type);
        }
    }

    /// <summary>
    /// Gets the guard flag associated with the provided field.
    /// </summary>
    /// <param name="field">The field being accessed.</param>
    /// <returns>The guard flag that will indicate whether the field is accessible.</returns>
    public FieldReference GetAccessibilityConstant(FieldReference field)
    {
        if (field.ContainsGenericParameter)
        {
            if (field.DeclaringType is GenericInstanceType git)
            {
                field = new FieldReference(field.Name, field.FieldType, git.ElementType);
            }
        }

        if (_fieldGuards.TryGetValue(field, out FieldReference? existing))
        {
            return existing;
        }

        var result = AddGuardField(field);
        _fieldGuards.Add(field, result);
        _il.Append(_il.Create(OpCodes.Ldtoken, field));
        _il.Append(_il.Create(OpCodes.Ldtoken, field.DeclaringType));
        _il.Append(_il.Create(OpCodes.Call, _references.CanAccess));
        _il.Append(_il.Create(OpCodes.Stsfld, result));
        return result;
    }

    /// <summary>
    /// Gets the guard flag associated with the provided method.
    /// </summary>
    /// <param name="method">The method being called.</param>
    /// <returns>The guard flag that will indicate whether the method is always accessible.</returns>
    public FieldReference GetAccessibilityConstant(MethodReference method)
    {
        if (method.ContainsGenericParameter)
        {
            method = method.GetElementMethod();

            if (method.DeclaringType is GenericInstanceType git)
            {
                method = new OverriddenDeclaringTypeMethod(method, git.ElementType);
            }
        }

        if (_methodGuards.TryGetValue(method, out FieldReference? existing))
        {
            return existing;
        }

        var result = AddGuardField(method);
        _methodGuards.Add(method, result);
        _il.Append(_il.Create(OpCodes.Ldtoken, method));
        _il.Append(_il.Create(OpCodes.Ldtoken, method.DeclaringType));
        _il.Append(_il.Create(OpCodes.Call, _references.CanCallAlways));
        _il.Append(_il.Create(OpCodes.Stsfld, result));
        return result;
    }

    /// <summary>
    /// Creates a new guard field. The guard field stores a flag indicating
    /// whether the provided member is always accessible from a sandboxed assembly.
    /// </summary>
    /// <param name="member">The member being guarded.</param>
    /// <returns>A reference to the static flag field.</returns>
    private FieldReference AddGuardField(MemberReference member)
    {
        var field = new FieldDefinition($".{member.Name}.{_fieldCount}",
            FieldAttributes.Public | FieldAttributes.Static | FieldAttributes.InitOnly, _references.BoolType);
        _type.Fields.Add(field);
        _fieldCount += 1;
        return field;
    }

    /// <summary>
    /// Decorates a method by replacing its declaring type with a different one.
    /// </summary>
    private sealed class OverriddenDeclaringTypeMethod : MethodReference
    {
        /// <summary>
        /// The inner method being called.
        /// </summary>
        private MethodReference _method;

        /// <summary>
        /// The declaring type that should be associated with the method.
        /// </summary>
        private TypeReference _overrideDeclaringType;

        /// <inheritdoc/>
        public override bool HasThis => _method.HasThis;

        /// <inheritdoc/>
        public override bool ExplicitThis => _method.ExplicitThis;

        /// <inheritdoc/>
        public override MethodCallingConvention CallingConvention => _method.CallingConvention;

        /// <inheritdoc/>
        public override bool HasParameters => _method.HasParameters;

        /// <inheritdoc/>
        public override Collection<ParameterDefinition> Parameters => _method.Parameters;

        /// <inheritdoc/>
        public override bool HasGenericParameters => _method.HasGenericParameters;

        /// <inheritdoc/>
        public override Collection<GenericParameter> GenericParameters => _method.GenericParameters;

        /// <inheritdoc/>
        public override bool IsGenericInstance => _method.IsGenericInstance;

        /// <inheritdoc/>
        public override bool ContainsGenericParameter => _method.ContainsGenericParameter;

        /// <inheritdoc/>
        public override string FullName => _method.FullName;

        /// <inheritdoc/>
        public override string Name => _method.Name;

        /// <inheritdoc/>
        public override bool IsDefinition => _method.IsDefinition;

        /// <inheritdoc/>
        public override MethodReturnType MethodReturnType => _method.MethodReturnType;

        /// <inheritdoc/>
        public override TypeReference DeclaringType => _overrideDeclaringType;

        /// <inheritdoc/>
        public override ModuleDefinition Module => _method.Module;

        /// <summary>
        /// Creates a new method reference that replaces the method's declaring
        /// type with the specified new type.
        /// </summary>
        /// <param name="method">The method.</param>
        /// <param name="declaringType">The new declaring type that the method should have.</param>
        public OverriddenDeclaringTypeMethod(MethodReference method, TypeReference declaringType)
            : base(method.Name, method.ReturnType)
        {
            _overrideDeclaringType = declaringType;
            _method = method;
            MetadataToken = method.MetadataToken;
        }

        /// <inheritdoc/>
        public sealed override MethodReference GetElementMethod()
        {
            return _method.GetElementMethod();
        }
    }
}
