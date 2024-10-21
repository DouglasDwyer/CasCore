using Mono.Cecil;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace DouglasDwyer.CasCore;

/// <summary>
/// Identifies a method using a stable hash of its signature.
/// </summary>
internal struct SignatureHash
{
    /// <summary>
    /// The hash value.
    /// </summary>
    private readonly ulong _value;

    /// <summary>
    /// Creates a new signature hash for the given method.
    /// </summary>
    /// <param name="method">The method.</param>
    public SignatureHash(MethodBase method)
    {
        Hash(method.DeclaringType!.Namespace, ref _value);
        Hash(method.DeclaringType!.Name, ref _value);

        Hash(method.Name, ref _value);

        foreach (var parameter in method.GetParameters())
        {
            var paramType = parameter.ParameterType;
            if (!paramType.IsGenericTypeParameter)
            {
                Hash(paramType.Namespace, ref _value);
                Hash(paramType.Name, ref _value);
            }
        }
    }

    /// <summary>
    /// Creates a new signature hash for the given method.
    /// </summary>
    /// <param name="method">The method.</param>
    public SignatureHash(MethodReference method)
    {
        Hash(method.DeclaringType!.Namespace, ref _value);
        Hash(method.DeclaringType!.Name, ref _value);

        Hash(method.Name, ref _value);

        foreach (var parameter in method.Parameters)
        {
            var paramType = parameter.ParameterType;
            if (!paramType.IsGenericParameter)
            {
                Hash(paramType.Namespace, ref _value);
                Hash(paramType.Name, ref _value);
            }
        }

    }

    /// <summary>
    /// Modifies the provided hash to include data from the given string.
    /// </summary>
    /// <param name="value">The string to include.</param>
    /// <param name="output">The hash reuslt.</param>
    private static void Hash(string? value, ref ulong output)
    {
        unsafe
        {
            const int charsPerNumber = sizeof(ulong) / sizeof(char);

            if (value is null)
            {
                return;
            }

            fixed (char* stringStart = value)
            {
                var numCount = (value.Length + charsPerNumber - 1) / charsPerNumber;
                var numbers = stackalloc ulong[numCount];
                Unsafe.CopyBlockUnaligned(numbers, stringStart, (uint)(sizeof(char) * value.Length));
                for (var i = 0; i < numCount; i++)
                {
                    var shift = i & 63;
                    output ^= (output << shift) ^ (*(numbers + i)) ^ (output >> shift);
                }
            }
        }
    }

    /// <inheritdoc/>
    public override bool Equals([NotNullWhen(true)] object? obj)
    {
        if (obj is SignatureHash other)
        {
            return this == other;
        }
        else
        {
            return false;
        }
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        return _value.GetHashCode();
    }

    /// <summary>
    /// Compares two member hashes for equality.
    /// </summary>
    /// <param name="lhs">The first member hash.</param>
    /// <param name="rhs">The second member hash.</param>
    /// <returns>Whether the hashes refer to the same member.</returns>
    public static bool operator ==(SignatureHash lhs, SignatureHash rhs)
    {
        return lhs._value == rhs._value;
    }

    /// <summary>
    /// Compares two member hashes for inequality.
    /// </summary>
    /// <param name="lhs">The first member hash.</param>
    /// <param name="rhs">The second member hash.</param>
    /// <returns>Whether the hashes refer to the same member.</returns>
    public static bool operator !=(SignatureHash lhs, SignatureHash rhs)
    {
        return !(lhs == rhs);
    }
}