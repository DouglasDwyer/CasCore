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
            foreach (var type in assembly.DefinedTypes
                .Where(x => !x.IsNested))
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

    private static Accessibility GetAccessibilityForType(Type type, Accessibility globalAccessibility)
    {
        if (type.IsPublic)
        {
            return globalAccessibility;
        }
        else if (globalAccessibility < Accessibility.Private)
        {
            return Accessibility.Public;
        }
        else
        {
            return Accessibility.Private;
        }
    }
}