using Mono.Cecil;
using System.Collections.Immutable;

namespace DouglasDwyer.CasCore;

/// <summary>
/// Member references necessary for adding a CAS hook.
/// </summary>
internal class ImportedReferences
{
    /// <summary>
    /// The <see cref="bool"/> type.
    /// </summary>
    public required TypeReference BoolType;

    /// <summary>
    /// Determines whether a field can be accessed.
    /// </summary>
    public required MethodReference CanAccess;

    /// <summary>
    /// Determines whether a method can always be called.
    /// </summary>
    public required MethodReference CanCallAlways;

    /// <summary>
    /// The runtime check for a field access.
    /// </summary>
    public required MethodReference CheckAccess;

    /// <summary>
    /// The runtime check for a virtual method call.
    /// </summary>
    public required MethodReference CheckVirtualCall;

    /// <summary>
    /// The runtime check for a constrained virtual method call.
    /// </summary>
    public required MethodReference CheckVirtualCallConstrained;

    /// <summary>
    /// The runtime check for delegate creation.
    /// </summary>
    public required MethodReference CreateCheckedDelegate;

    /// <summary>
    /// Invokes the violation handler for an invalid method call.
    /// </summary>
    public required MethodReference InvokeViolationHandler;

    /// <summary>
    /// The <see cref="object"/> type.
    /// </summary>
    public required TypeReference ObjectType;

    /// <summary>
    /// The methods that should be replaced with shims.
    /// </summary>
    public required IImmutableDictionary<SignatureHash, MethodReference> ShimmedMethods;

    /// <summary>
    /// The <see cref="void"/> type.
    /// </summary>
    public required TypeReference VoidType;
}