using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace DouglasDwyer.CasCore;

internal struct GuardWriter
{
    private readonly MethodDefinition _staticConstructor;
    private readonly ILProcessor _il;
    private readonly TypeDefinition _type;
    private readonly ImportedReferences _references;
    private int _fieldCount;

    public GuardWriter(MethodDefinition method, int id, ImportedReferences references)
    {
        _type = new TypeDefinition("CasCore.Guard", $"{method.Name}_{id}",
            TypeAttributes.Public | TypeAttributes.Class | TypeAttributes.AutoLayout
            | TypeAttributes.Abstract | TypeAttributes.Sealed, references.ObjectType);

        method.Module.Types.Add(_type);
        _staticConstructor = new MethodDefinition(".cctor", MethodAttributes.Public | MethodAttributes.HideBySig
            | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName | MethodAttributes.Static, references.VoidType);
        _type.Methods.Add(_staticConstructor);

        _il = _staticConstructor.Body.GetILProcessor();
        _fieldCount = 0;
        _references = references;
    }

    public void Finish()
    {
        _il.Append(_il.Create(OpCodes.Ret));
    }

    public FieldReference GetAccessibilityConstant(FieldReference field)
    {
        var result = AddGuardField(field);

        if (field.ContainsGenericParameter)
        {
            if (field.DeclaringType is GenericInstanceType git)
            {
                field = new FieldReference(field.Name, field.FieldType, git.ElementType);
            }
        }

        _il.Append(_il.Create(OpCodes.Ldtoken, field));
        _il.Append(_il.Create(OpCodes.Ldtoken, field.DeclaringType));
        _il.Append(_il.Create(OpCodes.Call, _references.CanAccess));
        _il.Append(_il.Create(OpCodes.Stsfld, result));
        return result;
    }

    public FieldReference GetAccessibilityConstant(MethodReference method)
    {
        var result = AddGuardField(method);

        if (method.ContainsGenericParameter)
        {
            method = method.GetElementMethod();

            if (method.DeclaringType is GenericInstanceType git)
            {
                method = new OverriddenDeclaringTypeMethod(method, git.ElementType);
            }
        }

        _il.Append(_il.Create(OpCodes.Ldtoken, method));
        _il.Append(_il.Create(OpCodes.Ldtoken, method.DeclaringType));
        _il.Append(_il.Create(OpCodes.Call, _references.CanCallAlways));
        _il.Append(_il.Create(OpCodes.Stsfld, result));
        return result;
    }

    private FieldReference AddGuardField(MemberReference member)
    {
        var field = new FieldDefinition($".{member.Name}.{_fieldCount}",
            FieldAttributes.Public | FieldAttributes.Static | FieldAttributes.InitOnly, _references.BoolType);
        _type.Fields.Add(field);
        _fieldCount += 1;
        return field;
    }

    private sealed class OverriddenDeclaringTypeMethod : MethodReference
    {
        private MethodReference _method;
        private TypeReference _overrideDeclaringType;

        public override bool HasThis => _method.HasThis;

        public override bool ExplicitThis => _method.ExplicitThis;

        public override MethodCallingConvention CallingConvention => _method.CallingConvention;

        public override bool HasParameters => _method.HasParameters;

        public override Collection<ParameterDefinition> Parameters => _method.Parameters;

        public override bool HasGenericParameters => _method.HasGenericParameters;

        public override Collection<GenericParameter> GenericParameters => _method.GenericParameters;

        public override bool IsGenericInstance => _method.IsGenericInstance;

        public override bool ContainsGenericParameter => _method.ContainsGenericParameter;

        public override string FullName => _method.FullName;

        public override string Name => _method.Name;

        public override bool IsDefinition => _method.IsDefinition;

        public override MethodReturnType MethodReturnType => _method.MethodReturnType;

        public override TypeReference DeclaringType => _overrideDeclaringType;

        public override ModuleDefinition Module => _method.Module;

        public OverriddenDeclaringTypeMethod(MethodReference method, TypeReference declaringType)
            : base(method.Name, method.ReturnType)
        {
            _overrideDeclaringType = declaringType;
            _method = method;
            MetadataToken = method.MetadataToken;
        }

        public sealed override MethodReference GetElementMethod()
        {
            return _method.GetElementMethod();
        }
    }
}
