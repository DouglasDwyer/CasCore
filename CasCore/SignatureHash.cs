using Mono.Cecil;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace DouglasDwyer.CasCore;

internal struct SignatureHash
{
    private readonly ulong _value;

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
    /// Compares two member IDs for equality.
    /// </summary>
    /// <param name="lhs">The first member ID.</param>
    /// <param name="rhs">The second member ID.</param>
    /// <returns>Whether the IDs refer to the same member.</returns>
    public static bool operator ==(SignatureHash lhs, SignatureHash rhs)
    {
        return lhs._value == rhs._value;
    }

    /// <summary>
    /// Compares two member IDs for inequality.
    /// </summary>
    /// <param name="lhs">The first member ID.</param>
    /// <param name="rhs">The second member ID.</param>
    /// <returns>Whether the IDs refer to the same member.</returns>
    public static bool operator !=(SignatureHash lhs, SignatureHash rhs)
    {
        return !(lhs == rhs);
    }
}