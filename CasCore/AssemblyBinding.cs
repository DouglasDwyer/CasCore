using System.Collections;
using System.Diagnostics;
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
                members.UnionWith(new TypeBinding(type, AccessibilityForType(type, accessibility)));
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

    /// <summary>
    /// Determines the accessibility level that should be used for members of an assembly.
    /// </summary>
    /// <param name="type">The target type.</param>
    /// <param name="parentAccessibility">The requested accessibility level for the assembly.</param>
    /// <returns>The accessibility that should be used.</returns>
    private static Accessibility AccessibilityForType(Type type, Accessibility parentAccessibility)
    {
        var privateButNotAccessible = !type.IsPublic && parentAccessibility != Accessibility.Private;
        if (type.IsClass || type.GetInterfaces().Any())
        {
            if (privateButNotAccessible)
            {
                return (Accessibility)Math.Min((int)parentAccessibility, (int)Accessibility.Public);
            }
            else
            {
                return parentAccessibility;
            }
        }
        else if (privateButNotAccessible)
        {
            return Accessibility.None;
        }
        else
        {
            return parentAccessibility;
        }
    }
}