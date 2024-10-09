using Mono.Cecil;
using Mono.Cecil.Cil;

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
        _il.Append(_il.Create(OpCodes.Ldtoken, field));
        _il.Append(_il.Create(OpCodes.Call, _references.CanAccess));
        _il.Append(_il.Create(OpCodes.Stsfld, result));
        return result;
    }

    public FieldReference GetAccessibilityConstant(MethodReference method)
    {
        var result = AddGuardField(method);
        _il.Append(_il.Create(OpCodes.Ldtoken, method));
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
}