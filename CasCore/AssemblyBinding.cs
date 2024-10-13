using System.Collections;
using System.Reflection;

namespace DouglasDwyer.CasCore;

/// <summary>
/// Facilitates selecting members of an assembly to include in a <see cref="CasPolicy"/>.
/// </summary>
public sealed class AssemblyBinding : IEnumerable<MemberInfo>
{
    /// <summary>
    /// The members to include in the assembly binding.
    /// </summary>
    private IEnumerable<MemberInfo> _members;

    /// <summary>
    /// Creates a new binding that selects all members with the provided visibility in the assembly.
    /// </summary>
    /// <param name="assembly">The assembly containing types to include.</param>
    /// <param name="accessibility">Which subset of members should be visible.</param>
    public AssemblyBinding(Assembly assembly, Accessibility accessibility)
    {
        var members = new HashSet<MemberInfo>();

        if (Accessibility.None < accessibility)
        {
            var targetTypes = assembly.DefinedTypes
                .Where(x => !x.IsNested);

            if (accessibility < Accessibility.Private)
            {
                targetTypes = targetTypes
                    .Where(x => x.IsPublic);
            }

            foreach (var type in targetTypes)
            {
                members.UnionWith(new TypeBinding(type, accessibility));
            }
        }

        _members = members;
    }

    /// <inheritdoc/>
    public IEnumerator<MemberInfo> GetEnumerator()
    {
        return _members.GetEnumerator();
    }

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}