﻿using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Security;

namespace DouglasDwyer.CasCore.Tests;

public static class TestMemorySafety
{
    [TestException(typeof(TypeInitializationException))]
    public static unsafe int TestInvalidPointerWrite()
    {
        var x = 1;
        var y = &x + 1;
        *y = 2;
        return x;
    }

    [TestException(typeof(TypeInitializationException))]
    public static unsafe int TestInvalidPointerRead()
    {
        var x = 1;
        var y = &x + 1;
        var z = *y;
        return z;
    }

    [TestException(typeof(TypeInitializationException))]
    public static unsafe int* TestInvalidStackalloc()
    {
        var data = stackalloc int[28];
        return data;
    }

    [TestSuccessful]
    public static unsafe int TestRefRead()
    {
        var x = 1;
        ref var y = ref x;
        return y;
    }

    [TestSuccessful]
    public static unsafe int TestRefWrite()
    {
        var x = 1;
        ref var y = ref x;
        y += 1;
        return x;
    }

    [TestException(typeof(SecurityException))]
    public static void TestGcAllocateUninitArray()
    {
        GC.AllocateUninitializedArray<int>(29);
    }

    [TestSuccessful]
    public static void TestGcAllocateArray()
    {
        GC.AllocateArray<int>(29);
    }

    [TestException(typeof(SecurityException))]
    public static void TestUnsafe()
    {
        var x = 29;
        Unsafe.Add(ref x, 29) = 30;
    }

    [TestException(typeof(SecurityException))]
    public static void TestEmit()
    {
        AssemblyBuilder.DefineDynamicAssembly(new AssemblyName(), AssemblyBuilderAccess.RunAndCollect);
    }

    [TestException(typeof(SecurityException))]
    public static void TestRuntimeHelpersGetUninitObject()
    {
        RuntimeHelpers.GetUninitializedObject(typeof(MethodBuilder));
    }
}