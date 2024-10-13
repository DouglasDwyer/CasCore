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
    /// Creates a new CAS policy that allows the provided set of members to be accessed.
    /// </summary>
    /// <param name="members">The set of allowed members.</param>
    internal CasPolicy(ImmutableHashSet<MemberId> members)
    {
        _accessibleMembers = members;
    }

    /// <summary>
    /// Determines whether this policy allows the specified field to be accessed.
    /// </summary>
    /// <param name="field">The field to be read/written.</param>
    /// <returns>Whether the field is accessible.</returns>
    internal bool CanAccess(FieldInfo field)
    {
        var memberId = new MemberId(field);
        return _accessibleMembers.Contains(memberId);
    }

    /// <summary>
    /// Determines whether this policy allows the specified method to be called.
    /// </summary>
    /// <param name="method">The method to be invoked.</param>
    /// <returns>Whether the method is accessible.</returns>
    internal bool CanAccess(MethodBase method)
    {
        var memberId = new MemberId(method);
        return _accessibleMembers.Contains(memberId);
    }
}