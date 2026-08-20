using System.Collections.Immutable;
using System.Reflection;

namespace DouglasDwyer.CasCore;

/// <summary>
/// Defines a set of whitelisted fields and methods that sandboxed assemblies may access.
/// </summary>
public sealed class CasPolicy
{
    /// <summary>
    /// The set of members that may be accessed under this policy.
    /// </summary>
    private readonly ImmutableHashSet<MemberId> _accessibleMembers;

    /// <summary>
    /// A mapping from a dangerous system method to its safe shim equivalent.
    /// </summary>
    private readonly ImmutableDictionary<MethodBase, MethodBase> _shims;

    /// <summary>
    /// A mapping from a dangerous system method's signature to its safe shim equivalent.
    /// </summary>
    private readonly IImmutableDictionary<SignatureHash, MethodBase> _signatureToShim;

    /// <summary>
    /// Creates a new CAS policy that allows the provided set of members to be accessed.
    /// </summary>
    /// <param name="members">The set of allowed members.</param>
    /// <param name="shims">Methods that should be replaced with safe equivalents.</param>
    internal CasPolicy(ImmutableHashSet<MemberId> members, ImmutableDictionary<MethodBase, MethodBase> shims)
    {
        _accessibleMembers = members;
        _shims = shims;
        _signatureToShim = _shims.ToImmutableDictionary(x => new SignatureHash(x.Key), x => x.Value);
    }

    /// <summary>
    /// Determines whether this policy allows the specified field to be accessed.
    /// </summary>
    /// <param name="field">The field to be read/written.</param>
    /// <returns>Whether the field is accessible.</returns>
    public bool CanAccess(FieldInfo field)
    {
        var memberId = new MemberId(field);
        return _accessibleMembers.Contains(memberId);
    }

    /// <summary>
    /// Determines whether this policy allows the specified method to be called.
    /// This will return <see langword="false"/> for shimmed methods.
    /// </summary>
    /// <param name="method">The method to be invoked.</param>
    /// <returns>Whether the method is accessible.</returns>
    public bool CanAccess(MethodBase method)
    {
        var memberId = new MemberId(method);
        return _accessibleMembers.Contains(memberId);
    }

    /// <summary>
    /// Obtains the shim that replaces the given method.
    /// </summary>
    /// <param name="method">The original method to be replaced.</param>
    /// <returns>The shim to use, or <see langword="null"/> if the method did not have a shim.</returns>
    public MethodBase? GetShim(MethodBase method)
    {
        return _shims.GetValueOrDefault(method);
    }
}