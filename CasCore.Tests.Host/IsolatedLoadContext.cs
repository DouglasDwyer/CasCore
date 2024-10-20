using DouglasDwyer.CasCore.Tests.Shared;
using System.Reflection;

namespace DouglasDwyer.CasCore.Tests.Host;

class IsolatedLoadContext : CasAssemblyLoader
{
    private readonly string[] _sharedAssemblies;

    public IsolatedLoadContext(string[] sharedAssemblies, CasPolicy policy) : base(policy)
    {
        _sharedAssemblies = sharedAssemblies.ToArray();
    }

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        var existingAssembly = base.Load(assemblyName);

        if (existingAssembly is not null)
        {
            return existingAssembly;
        }

        // Check for shared assemblies, return null because they'll be loaded by default AssemblyLoadContext 
        if (_sharedAssemblies.Contains(assemblyName.Name))
        {
            return null;
        }

        throw new NotSupportedException($"Unable to load managed assembly: {assemblyName.Name}");
    }

    protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
    {
        throw new NotSupportedException($"Unable to load unmanaged assembly: {unmanagedDllName}");
    }

    public static IsolatedLoadContext CreateDefault()
    {
        var policy = new CasPolicyBuilder()
            .WithDefaultSandbox()
            .Allow(new TypeBinding(typeof(SharedClass), Accessibility.None)
                .WithConstructor([], Accessibility.Public)
                .WithField("AllowedStaticField", Accessibility.Public)
                .WithField("AllowedField", Accessibility.Public))
            .Build();

        return new IsolatedLoadContext([
            "CasCore.Tests.Shared",
            "System.Collections.Concurrent",
            "System.Collections",
            "System.Collections.Immutable",
            "System.Collections.NonGeneric",
            "System.Collections.Specialized",
            "System.ComponentModel.Annotations",
            "System.ComponentModel.DataAnnotations",
            "System.ComponentModel",
            "System.ComponentModel.EventBasedAsync",
            "System.ComponentModel.Primitives",
            "System.ComponentModel.TypeConverter",
            "System.Configuration",
            "System.Console",
            "System.Core",
            "System.Data.Common",
            "System.Data.DataSetExtensions",
            "System.Data",
            "System.Diagnostics.Contracts",
            "System.Diagnostics.Debug",
            "System.Diagnostics.DiagnosticSource",
            "System.Diagnostics.FileVersionInfo",
            "System.Diagnostics.StackTrace",
            "System.Diagnostics.TextWriterTraceListener",
            "System.Diagnostics.Tools",
            "System.Diagnostics.TraceSource",
            "System.Diagnostics.Tracing",
            "System",
            "System.Drawing",
            "System.Drawing.Primitives",
            "System.Dynamic.Runtime",
            "System.Formats.Asn1",
            "System.Formats.Tar",
            "System.Globalization.Calendars",
            "System.Globalization",
            "System.Globalization.Extensions",
            "System.IO.Compression.Brotli",
            "System.IO.Compression",
            "System.IO.Compression.FileSystem",
            "System.IO.Compression.ZipFile",
            "System.IO",
            "System.IO.FileSystem.AccessControl",
            "System.IO.FileSystem",
            "System.IO.FileSystem.DriveInfo",
            "System.IO.FileSystem.Primitives",
            "System.IO.FileSystem.Watcher",
            "System.IO.IsolatedStorage",
            "System.IO.MemoryMappedFiles",
            "System.IO.Pipes.AccessControl",
            "System.IO.Pipes",
            "System.IO.UnmanagedMemoryStream",
            "System.Linq",
            "System.Linq.Expressions",
            "System.Linq.Parallel",
            "System.Linq.Queryable",
            "System.Memory",
            "System.Numerics",
            "System.Numerics.Vectors",
            "System.ObjectModel",
            "System.Private.CoreLib",
            "System.Private.DataContractSerialization",
            "System.Private.Uri",
            "System.Private.Xml",
            "System.Private.Xml.Linq",
            "System.Reflection.DispatchProxy",
            "System.Reflection",
            "System.Reflection.Emit",
            "System.Reflection.Emit.ILGeneration",
            "System.Reflection.Emit.Lightweight",
            "System.Reflection.Extensions",
            "System.Reflection.Metadata",
            "System.Reflection.Primitives",
            "System.Reflection.TypeExtensions",
            "System.Resources.Reader",
            "System.Resources.ResourceManager",
            "System.Resources.Writer",
            "System.Runtime",
            "System.Runtime.Extensions",
            "System.Runtime.Handles",
            "System.Runtime.InteropServices",
            "System.Runtime.InteropServices.RuntimeInformation",
            "System.Runtime.Intrinsics",
            "System.Runtime.Loader",
            "System.Runtime.Numerics",
            "System.Runtime.Serialization",
            "System.Runtime.Serialization.Formatters",
            "System.Runtime.Serialization.Json",
            "System.Runtime.Serialization.Primitives",
            "System.Runtime.Serialization.Xml",
            "System.Security.AccessControl",
            "System.Security.Claims",
            "System.Security.Cryptography.Algorithms",
            "System.Security.Cryptography.Cng",
            "System.Security.Cryptography.Csp",
            "System.Security.Cryptography",
            "System.Security.Cryptography.Encoding",
            "System.Security.Cryptography.OpenSsl",
            "System.Security.Cryptography.Primitives",
            "System.Security.Cryptography.X509Certificates",
            "System.Security",
            "System.Security.Principal",
            "System.Security.Principal.Windows",
            "System.Security.SecureString",
            "System.ServiceModel.Web",
            "System.ServiceProcess",
            "System.Text.Encoding.CodePages",
            "System.Text.Encoding",
            "System.Text.Encoding.Extensions",
            "System.Text.Encodings.Web",
            "System.Text.Json",
            "System.Text.RegularExpressions",
            "System.Threading.Channels",
            "System.Threading",
            "System.Threading.Overlapped",
            "System.Threading.Tasks.Dataflow",
            "System.Threading.Tasks",
            "System.Threading.Tasks.Extensions",
            "System.Threading.Tasks.Parallel",
            "System.Threading.Thread",
            "System.Threading.ThreadPool",
            "System.Threading.Timer",
            "System.Transactions",
            "System.Transactions.Local",
            "System.ValueTuple",
            "System.Xml",
            "System.Xml.Linq",
            "System.Xml.ReaderWriter",
            "System.Xml.Serialization",
            "System.Xml.XDocument",
            "System.Xml.XmlDocument",
            "System.Xml.XmlSerializer",
            "System.Xml.XPath",
            "System.Xml.XPath.XDocument",
            "netstandard"
         ], policy);
    }
}