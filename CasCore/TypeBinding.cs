
using System.Reflection;

namespace DouglasDwyer.CasCore;

public sealed class TypeBinding : MemberBinding
{
    internal override IEnumerable<MemberId> Members => _selectedMembers;

    private readonly HashSet<MemberId> _selectedMembers = new HashSet<MemberId>();
    private readonly Type _type;

    public TypeBinding(Type type, CasBindingFlags flags)
    {
        _type = type;
        var bindingFlags = flags.GetBindingFlags();

        if (flags.HasFlag(CasBindingFlags.Field))
        {
            _selectedMembers.UnionWith(_type.GetFields(bindingFlags).Select(x => new MemberId(x)));
        }

        if (flags.HasFlag(CasBindingFlags.Constructor))
        {
            _selectedMembers.UnionWith(_type.GetConstructors(bindingFlags).Select(x => new MemberId(x)));
        }

        if (flags.HasFlag(CasBindingFlags.Method))
        {
            _selectedMembers.UnionWith(_type.GetMethods(bindingFlags).Select(x => new MemberId(x)));
        }
    }

    public TypeBinding AddConstructor()
    {
        var method = _type.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly);
        if (method.Count() != 1)
        {
            throw new ArgumentException($"Could not find constructor to add to binding for {_type}.");
        }
        _selectedMembers.Add(new MemberId(method.First()));
        return this;
    }

    public TypeBinding AddConstructor(Type[] parameters)
    {
        var method = _type.GetConstructor(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly, parameters);
        if (method is null)
        {
            throw new ArgumentException($"Could not find constructor to add to binding for {_type}.");
        }
        _selectedMembers.Add(new MemberId(method));
        return this;
    }

    public TypeBinding AddField(string name)
    {
        var field = _type.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly);
        if (field is null)
        {
            throw new ArgumentException($"Could not find field {name} to add to binding for {_type}.");
        }
        _selectedMembers.Add(new MemberId(field));
        return this;
    }

    public TypeBinding AddMethod(string name)
    {
        var method = _type.GetMethod(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly);
        if (method is null)
        {
            throw new ArgumentException($"Could not find method {name} to add to binding for {_type}.");
        }

        _selectedMembers.Add(new MemberId(method));

        return this;
    }

    public TypeBinding AddMethod(string name, Type[] parameters)
    {
        var method = _type.GetMethod(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly, parameters);
        if (method is null)
        {
            throw new ArgumentException($"Could not find method {name} to add to binding for {_type}.");
        }

        _selectedMembers.Add(new MemberId(method));

        return this;
    }

    public TypeBinding RemoveConstructor()
    {
        var method = _type.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly);
        if (method.Count() != 1)
        {
            throw new ArgumentException($"Could not find constructor to add to binding for {_type}.");
        }
        _selectedMembers.Remove(new MemberId(method.First()));
        return this;
    }

    public TypeBinding RemoveConstructor(Type[] parameters)
    {
        var method = _type.GetConstructor(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly, parameters);
        if (method is null)
        {
            throw new ArgumentException($"Could not find constructor to remove from binding for {_type}.");
        }
        _selectedMembers.Remove(new MemberId(method));
        return this;
    }

    public TypeBinding RemoveField(string name)
    {
        var field = _type.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly);
        if (field is null)
        {
            throw new ArgumentException($"Could not find field {name} to remove from binding for {_type}.");
        }
        _selectedMembers.Remove(new MemberId(field));
        return this;
    }

    public TypeBinding RemoveMethod(string name)
    {
        var method = _type.GetMethod(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly);
        if (method is null)
        {
            throw new ArgumentException($"Could not find method {name} to remove from binding for {_type}.");
        }

        _selectedMembers.Remove(new MemberId(method));

        return this;
    }

    public TypeBinding RemoveMethod(string name, Type[] parameters)
    {
        var method = _type.GetMethod(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly, parameters);
        if (method is null)
        {
            throw new ArgumentException($"Could not find method {name} to remove from binding for {_type}.");
        }

        _selectedMembers.Remove(new MemberId(method));

        return this;
    }
}