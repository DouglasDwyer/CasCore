using DouglasDwyer.CasCore.Tests.Shared;
using System.Runtime.CompilerServices;

namespace DouglasDwyer.CasCore.Tests.Csharp;

public static class TestRuntimeFeature
{
    [TestSuccessful]
    public static void IsDynamicCodeSupportedReportsFalse()
    {
        if (RuntimeFeature.IsDynamicCodeSupported)
        {
            throw new Exception("Expected RuntimeFeature.IsDynamicCodeSupported to be shimmed to false for a sandboxed assembly.");
        }
    }

    [TestSuccessful]
    public static void IsDynamicCodeCompiledReportsFalse()
    {
        if (RuntimeFeature.IsDynamicCodeCompiled)
        {
            throw new Exception("Expected RuntimeFeature.IsDynamicCodeCompiled to be shimmed to false for a sandboxed assembly.");
        }
    }

    [TestSuccessful]
    public static void IsSupportedDynamicCodeSupportedReportsFalse()
    {
        if (RuntimeFeature.IsSupported(nameof(RuntimeFeature.IsDynamicCodeSupported)))
        {
            throw new Exception("Expected RuntimeFeature.IsSupported(\"IsDynamicCodeSupported\") to be shimmed to false for a sandboxed assembly.");
        }
    }

    [TestSuccessful]
    public static void IsSupportedDynamicCodeCompiledReportsFalse()
    {
        if (RuntimeFeature.IsSupported(nameof(RuntimeFeature.IsDynamicCodeCompiled)))
        {
            throw new Exception("Expected RuntimeFeature.IsSupported(\"IsDynamicCodeCompiled\") to be shimmed to false for a sandboxed assembly.");
        }
    }

    [TestSuccessful]
    public static void IsSupportedDelegatesForUnrelatedFeatures()
    {
        // "PortablePdb" is unconditionally true in RuntimeFeature.IsSupported, unrelated to dynamic
        // code generation. This should pass through to the real implementation, unlike the two checks above.
        if (!RuntimeFeature.IsSupported("PortablePdb"))
        {
            throw new Exception("Expected RuntimeFeature.IsSupported(\"PortablePdb\") to delegate to the real implementation.");
        }
    }

    [TestSuccessful]
    public static void IsSupportedDelegatesForUnknownFeatures()
    {
        if (RuntimeFeature.IsSupported("SomeFeatureThatDoesNotExist"))
        {
            throw new Exception("Expected RuntimeFeature.IsSupported to delegate to the real implementation for unrecognized feature names.");
        }
    }
}
