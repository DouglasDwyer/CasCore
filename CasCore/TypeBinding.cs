
using System.Collections;
using System.Reflection;

namespace DouglasDwyer.CasCore;

/// <summary>
/// Facilitates selecting particular members of a type to include in a <see cref="CasPolicy"/>.
/// </summary>
public sealed class TypeBinding : IEnumerable<MemberInfo>
{
    /// <summary>
    /// The flags to use when iterating over members with reflection.
    /// </summary>
    private const BindingFlags AllMemberFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

    /// <summary>
    /// The members currently selected by this binding.
    /// </summary>
    private readonly HashSet<MemberInfo> _selectedMembers = new HashSet<MemberInfo>();

    /// <summary>
    /// The type currently selected by this binding.
    /// </summary>
    private readonly Type _type;

    /// <summary>
    /// Creates a new binding for members of the given type. This recursively includes members of nested classes
    /// that are visible under the given accessibility.
    /// </summary>
    /// <param name="type">The target type.</param>
    /// <param name="accessibility">The accessibility level of members.</param>
    public TypeBinding(Type type, Accessibility accessibility)
    {
        _type = type;
        AddMembersForType(_type, accessibility);
    }

    /// <summary>
    /// Adds the constructor of the given accessibility to this binding.
    /// </summary>
    /// <param name="accessibility">The target member's accessibility.</param>
    /// <returns>This type binding, with the member added.</returns>
    /// <exception cref="ArgumentException">If no member could be found.</exception>
    public TypeBinding WithConstructor(Accessibility accessibility)
    {
        var methods = _type.GetConstructors(AllMemberFlags).Where(x => MethodAccessible(x, accessibility));
        
        if (methods.Count() != 1)
        {
            throw new ArgumentException($"Could not find constructor to add to binding for {_type}.");
        }

        _selectedMembers.Add(methods.First());
        return this;
    }

    /// <summary>
    /// Adds the constructor with the given parameters and accessibility to this binding.
    /// </summary>
    /// <param name="parameters">The parameters that the constructor accepts.</param>
    /// <param name="accessibility">The target member's accessibility.</param>
    /// <returns>This type binding, with the member added.</returns>
    /// <exception cref="ArgumentException">If no member could be found.</exception>
    public TypeBinding WithConstructor(Type[] parameters, Accessibility accessibility)
    {
        try
        {
            var method = _type.GetConstructor(AllMemberFlags, parameters);
            if (method is not null && MethodAccessible(method, accessibility))
            {
                _selectedMembers.Add(method);
                return this;
            }
        }
        catch (AmbiguousMatchException) {}

        var methods = _type.GetConstructors(AllMemberFlags)
                .Where(x => x.GetParameters().Select(y => GenericBaseIfContainsGeneric(y.ParameterType)).SequenceEqual(parameters))
                .Where(x => MethodAccessible(x, accessibility));

        if (methods.Count() == 0)
        {
            throw new ArgumentException($"Could not find constructor to add to binding for {_type}.");
        }

        _selectedMembers.Add(methods.First());
        return this;

    }

    /// <summary>
    /// Adds the field with the given name and accessibility to this binding.
    /// </summary>
    /// <param name="name">The name of the member to add.</param>
    /// <param name="accessibility">The target member's accessibility.</param>
    /// <returns>This type binding, with the member added.</returns>
    /// <exception cref="ArgumentException">If no member could be found.</exception>
    public TypeBinding WithField(string name, Accessibility accessibility)
    {
        var field = _type.GetField(name, AllMemberFlags);
        if (field is null || !FieldAccessible(field, accessibility))
        {
            throw new ArgumentException($"Could not find field {name} to add to binding for {_type}.");
        }
        _selectedMembers.Add(field);
        return this;
    }

    /// <summary>
    /// Adds the method with the given name and accessibility to this binding.
    /// </summary>
    /// <param name="name">The name of the member to add.</param>
    /// <param name="accessibility">The target member's accessibility.</param>
    /// <returns>This type binding, with the member added.</returns>
    /// <exception cref="ArgumentException">If no member could be found.</exception>
    public TypeBinding WithMethod(string name, Accessibility accessibility)
    {
        try
        {
            var method = _type.GetMethod(name, AllMemberFlags);
            if (method is not null && MethodAccessible(method, accessibility))
            {
                _selectedMembers.Add(method);
                return this;
            }
        }
        catch (AmbiguousMatchException) { }

        var methods = _type.GetMethods(AllMemberFlags)
                .Where(x => x.Name == name)
                .Where(x => MethodAccessible(x, accessibility));

        if (methods.Count() == 0)
        {
            throw new ArgumentException($"Could not find method {name} to add to binding for {_type}.");
        }

        _selectedMembers.Add(methods.First());
        return this;
    }

    /// <summary>
    /// Adds the method with the given name and accessibility to this binding.
    /// </summary>
    /// <param name="name">The name of the member to add.</param>
    /// <param name="parameters">The parameters that the method accepts.</param>
    /// <param name="accessibility">The target member's accessibility.</param>
    /// <returns>This type binding, with the member added.</returns>
    /// <exception cref="ArgumentException">If no member could be found.</exception>
    public TypeBinding WithMethod(string name, Type[] parameters, Accessibility accessibility)
    {
        try
        {
            var method = _type.GetMethod(name, AllMemberFlags);
            if (method is not null && MethodAccessible(method, accessibility))
            {
                _selectedMembers.Add(method);
                return this;
            }
        }
        catch (AmbiguousMatchException) { }

        var methods = _type.GetMethods(AllMemberFlags)
                .Where(x => x.Name == name)
                .Where(x => x.GetParameters().Select(y => GenericBaseIfContainsGeneric(y.ParameterType)).SequenceEqual(parameters))
                .Where(x => MethodAccessible(x, accessibility));

        if (methods.Count() == 0)
        {
            throw new ArgumentException($"Could not find method {name} to add to binding for {_type}.");
        }

        _selectedMembers.Add(methods.First());
        return this;
    }

    /// <inheritdoc/>
    public IEnumerator<MemberInfo> GetEnumerator()
    {
        return _selectedMembers.GetEnumerator();
    }

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    /// <summary>
    /// Adds all accessible members for the provided type, including members of nested types.
    /// </summary>
    /// <param name="type">The type to include.</param>
    /// <param name="accessibility">The accessibility level of members to include.</param>
    private void AddMembersForType(Type type, Accessibility accessibility)
    {
        if (accessibility == Accessibility.None)
        {
            return;
        }

        _selectedMembers.UnionWith(type.GetFields(AllMemberFlags).Where(x => FieldAccessible(x, accessibility)));
        _selectedMembers.UnionWith(type.GetConstructors(AllMemberFlags).Where(x => MethodAccessible(x, accessibility)));
        _selectedMembers.UnionWith(type.GetMethods(AllMemberFlags).Where(x => MethodAccessible(x, accessibility)));
        
        foreach (var nested in type.GetNestedTypes())
        {
            AddMembersForType(nested, AccessibilityForNestedType(nested, accessibility));
        }
    }

    /// <summary>
    /// Determines the accessibility level that should be used for members of a nested type.
    /// </summary>
    /// <param name="type">The target nested type.</param>
    /// <param name="parentAccessibility">The requested accessibility level for the parent type.</param>
    /// <returns>The accessibility that should be used.</returns>
    private static Accessibility AccessibilityForNestedType(Type type, Accessibility parentAccessibility)
    {
        var privateType = !(type.IsNestedPublic || type.IsNestedFamily);
        if (privateType && parentAccessibility != Accessibility.Private)
        {
            return (Accessibility)Math.Min((int)parentAccessibility, (int)Accessibility.Public);
        }
        else
        {
            return parentAccessibility;
        }
    }

    /// <summary>
    /// Determines whether a field should be visible under the given accessibility.
    /// </summary>
    /// <param name="field">The member in question.</param>
    /// <param name="accessibility">The target accessibility.</param>
    /// <returns>Whether the member should be accessible.</returns>
    private static bool FieldAccessible(FieldInfo field, Accessibility accessibility)
    {
        return (field.IsPublic && Accessibility.Public <= accessibility)
            || ((field.IsFamily || field.IsFamilyOrAssembly) && Accessibility.Protected <= accessibility)
            || Accessibility.Private <= accessibility;
    }

    /// <summary>
    /// Determines whether a method should be visible under the given accessibility.
    /// </summary>
    /// <param name="field">The member in question.</param>
    /// <param name="accessibility">The target accessibility.</param>
    /// <returns>Whether the member should be accessible.</returns>
    private static bool MethodAccessible(MethodBase method, Accessibility accessibility)
    {
        return (method.IsPublic && Accessibility.Public <= accessibility)
            || ((method.IsFamily || method.IsFamilyOrAssembly) && Accessibility.Protected <= accessibility)
            || Accessibility.Private <= accessibility;
    }

    private Type GenericBaseIfContainsGeneric(Type type)
    {
        if (type.ContainsGenericParameters)
        {
            try
            {
                return type.GetGenericTypeDefinition();
            }
            catch { }
        }

        return type;
    }
}