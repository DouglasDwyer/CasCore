using System.Collections.Immutable;
using System.Reflection;

namespace DouglasDwyer.CasCore;

public sealed class CasPolicyBuilder
{
    private readonly HashSet<MemberId> _accessibleMembers;

    public CasPolicyBuilder()
    {
        _accessibleMembers = new HashSet<MemberId>();
    }

    public CasPolicy Build()
    {
        return new CasPolicy(_accessibleMembers.ToImmutableHashSet());
    }

    public CasPolicyBuilder Allow(MemberInfo member)
    {
        _accessibleMembers.Add(new MemberId(member));
        return this;
    }

    public CasPolicyBuilder Allow(MemberBinding binding)
    {
        _accessibleMembers.UnionWith(binding.Members);
        return this;
    }

    public CasPolicyBuilder Deny(MemberInfo member)
    {
        _accessibleMembers.Remove(new MemberId(member));
        return this;
    }

    public CasPolicyBuilder Deny(MemberBinding binding)
    {
        _accessibleMembers.ExceptWith(binding.Members);
        return this;
    }
}