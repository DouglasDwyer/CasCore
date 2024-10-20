using DouglasDwyer.CasCore.Tests.Host;

internal class Program
{
    public static void Main()
    {
        var loadContext = IsolatedLoadContext.CreateDefault();
        loadContext.LoadFromAssemblyPath("Newtonsoft.Json.dll");
        var testAssy = loadContext.LoadFromStream(new FileStream("CasCore.Tests.dll", FileMode.Open), new FileStream("CasCore.Tests.pdb", FileMode.Open));
        testAssy.GetType("DouglasDwyer.CasCore.Tests.TestRunner")!.GetMethod("Run")!.Invoke(null, []);
    }
}