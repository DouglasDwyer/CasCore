using Mono.Cecil;
using System.Collections.Immutable;

namespace DouglasDwyer.CasCore;

/// <summary>
/// Member references necessary for adding a CAS hook.
/// </summary>
internal class ImportedReferences
{
    public required IImmutableDictionary<SignatureHash, MethodReference> ShimmedMethods;
    public required MethodReference AssertCanAccess;
    public required MethodReference AssertCanCall;
    public required MethodReference AssertCanCallConstrained;
    public required MethodReference CreateCheckedDelegate;
    public required TypeReference BoolType;
    public required MethodReference CanAccess;
    public required MethodReference CanCallAlways;
    public required TypeReference ObjectType;
    public required TypeReference VoidType;
}