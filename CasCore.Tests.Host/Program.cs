namespace DouglasDwyer.CasCore.Tests.Host;

using DouglasDwyer.CasCore;
using DouglasDwyer.CasCore.Tests.Shared;

internal class Program
{
    public static void Main()
    {
        var policy = new CasPolicyBuilder()
            .WithDefaultSandbox()
            .Allow(new TypeBinding(typeof(SharedClass), Accessibility.None)
                .WithConstructor([], Accessibility.Public)
                .WithField("AllowedStaticField", Accessibility.Public)
                .WithField("AllowedField", Accessibility.Public)
                .WithMethod("InterfaceMethod", Accessibility.Public))
            .Allow(new TypeBinding(typeof(SharedClass.SharedNested), Accessibility.None)
                .WithConstructor([], Accessibility.Public)
                .WithMethod("VirtualMethod", Accessibility.Public))
            .Build();

        var loadContext = new CasAssemblyLoader(policy);
        loadContext.LoadFromStream(new FileStream("Newtonsoft.Json.dll", FileMode.Open));
        var testAssy = loadContext.LoadFromStream(new FileStream("CasCore.Tests.dll", FileMode.Open), new FileStream("CasCore.Tests.pdb", FileMode.Open));
        testAssy.GetType("DouglasDwyer.CasCore.Tests.TestRunner")!.GetMethod("Run")!.Invoke(null, []);
    }
}