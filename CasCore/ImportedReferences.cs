using Mono.Cecil;
using System.Collections.Immutable;

namespace DouglasDwyer.CasCore;

/// <summary>
/// Member references necessary for adding a CAS hook.
/// </summary>
internal class ImportedReferences
{
    /// <summary>
    /// The methods that should be replaced with shims.
    /// </summary>
    public required IImmutableDictionary<SignatureHash, MethodReference> ShimmedMethods;

    /// <summary>
    /// The runtime check for a field access.
    /// </summary>
    public required MethodReference AssertCanAccess;

    /// <summary>
    /// The runtime check for a method call.
    /// </summary>
    public required MethodReference AssertCanCall;

    /// <summary>
    /// The runtime check for a constrained method call.
    /// </summary>
    public required MethodReference AssertCanCallConstrained;

    /// <summary>
    /// The <see cref="bool"/> type.
    /// </summary>
    public required TypeReference BoolType;

    /// <summary>
    /// The runtime check for delegate creation.
    /// </summary>
    public required MethodReference CreateCheckedDelegate;

    /// <summary>
    /// Determines whether a field can be accessed.
    /// </summary>
    public required MethodReference CanAccess;

    /// <summary>
    /// Determines whether a method can always be called.
    /// </summary>
    public required MethodReference CanCallAlways;

    /// <summary>
    /// The <see cref="object"/> type.
    /// </summary>
    public required TypeReference ObjectType;

    /// <summary>
    /// The <see cref="void"/> type.
    /// </summary>
    public required TypeReference VoidType;
}