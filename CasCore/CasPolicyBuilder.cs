using System.Collections.Immutable;
using System.Reflection;

namespace DouglasDwyer.CasCore;

/// <summary>
/// Allows for specifying the list of fields and methods that a sandboxed assembly may access.
/// </summary>
public sealed class CasPolicyBuilder
{
    /// <summary>
    /// The list of members selected by the policy builder.
    /// </summary>
    private readonly HashSet<MemberId> _accessibleMembers;

    /// <summary>
    /// Creates a new policy builder. By default, all access to external APIs is denied.
    /// </summary>
    public CasPolicyBuilder()
    {
        _accessibleMembers = new HashSet<MemberId>();
    }

    /// <summary>
    /// Bakes the list of accessible members into a <see cref="CasPolicy"/> that may be used to instantiate assemblies.
    /// </summary>
    /// <returns>The policy that was generated.</returns>
    public CasPolicy Build()
    {
        return new CasPolicy(_accessibleMembers.ToImmutableHashSet());
    }

    /// <summary>
    /// Allows the given member to be accessed by sandboxed assemblies.
    /// </summary>
    /// <param name="member">The member to allow.</param>
    /// <returns>This builder, with the additional member added.</returns>
    public CasPolicyBuilder Allow(MemberInfo member)
    {
        _accessibleMembers.Add(new MemberId(member));
        return this;
    }

    /// <summary>
    /// Allows the given set of members to be accessed by sandboxed assemblies.
    /// </summary>
    /// <param name="members">The members to allow.</param>
    /// <returns>This builder, with the additional members added.</returns>
    public CasPolicyBuilder Allow(IEnumerable<MemberInfo> members)
    {
        _accessibleMembers.UnionWith(members.Select(x => new MemberId(x)));
        return this;
    }

    /// <summary>
    /// Prevents the given member from being accessed by sandboxed assemblies.
    /// </summary>
    /// <param name="member">The member to deny.</param>
    /// <returns>This builder, with the member removed if it was present.</returns>
    public CasPolicyBuilder Deny(MemberInfo member)
    {
        _accessibleMembers.Remove(new MemberId(member));
        return this;
    }

    /// <summary>
    /// Prevents the given members from being accessed by sandboxed assemblies.
    /// </summary>
    /// <param name="members">The members to deny.</param>
    /// <returns>This builder, with any relevant members removed if they were present.</returns>
    public CasPolicyBuilder Deny(IEnumerable<MemberInfo> members)
    {
        _accessibleMembers.ExceptWith(members.Select(x => new MemberId(x)));
        return this;
    }
}