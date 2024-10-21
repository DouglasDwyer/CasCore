# CasCore

[![NuGet version (DouglasDwyer.CasCore)](https://img.shields.io/nuget/v/DouglasDwyer.CasCore.svg?style=flat-square)](https://www.nuget.org/packages/DouglasDwyer.CasCore/)

#### Assembly-level sandboxing and Code Access Security for .NET Core

`CasCore` allows for securely executing untrusted C# code in an application. When loading an assembly, `CasCore` modifies the assembly's bytecode to add security checks. These checks prevent the assembly from violating memory safety or accessing resources without permission. Any assembly loaded with `CasCore` is subject to the following restrictions:

- The CIL bytecode of the assembly's methods must be valid and verifiable. Any unverifiable method will throw a [`TypeInitializationException`](https://learn.microsoft.com/en-us/dotnet/api/system.typeinitializationexception) when called.
- The assembly may only access a field if it exists within the same assembly, another assembly in the same [`AssemblyLoadContext`](https://learn.microsoft.com/en-us/dotnet/api/system.runtime.loader.assemblyloadcontext), or on the assembly's `CasPolicy` whitelist. Any attempt to read/write an inaccessible field fails with a [`System.SecurityException`](https://learn.microsoft.com/en-us/dotnet/api/system.security.securityexception).
- The assembly may only call a method if it exists within the same assembly, another assembly in the same [`AssemblyLoadContext`](https://learn.microsoft.com/en-us/dotnet/api/system.runtime.loader.assemblyloadcontext), or on the assembly's `CasPolicy` whitelist. Attempts to call inaccessible methods also fail with an exception.
- The assembly may only use reflection APIs to access allowed fields/methods. Attempts with reflection to access invalid fields/methods (according to the same rules as above) also fail with an exception.
- The assembly may only create delegates for allowed methods; attempts to create delegates for invalid methods also fail with an exception.
- The assembly may only use [`LambdaExpression.Compile`](https://learn.microsoft.com/en-us/dotnet/api/system.linq.expressions.lambdaexpression.compile) for expression trees with allowed fields/methods; attempts to create and execute expression trees with invalid methods also fail with an exception.

`CasCore` is meant as a replacement for [Code Access Security (CAS)](https://learn.microsoft.com/en-us/dotnet/framework/data/adonet/code-access-security), which was deprecated in .NET Core. `CasCore` was designed primarily for game modding/plugin systems, which may download third-party mods onto a user's computer. `CasCore` can prevent mods from performing malicious actions like writing to the filesystem or accessing the network.

### Installation

`CasCore` can be obtained as a Nuget package. Either run `dotnet add package DouglasDwyer.CasCore` via the command line, or download the library from the Visual Studio package manager.

### How to use

Sandboxing assemblies with `CasCore` involves two steps: defining a `CasPolicy`, then loading assemblies under that policy.

The `CasPolicy` defines a whitelist of fields and methods that sandboxed assemblies may access. Any external member accesses not explicitly on the policy whitelist will fail with a [`System.SecurityException`](https://learn.microsoft.com/en-us/dotnet/api/system.security.securityexception). Policies are created using the `CasPolicyBuilder` class:

```csharp
var policy = new CasPolicyBuilder()   // Create a new, empty whitelist.
    .WithDefaultSandbox()   // Add all system types that are on the default whitelist
    .Allow(new TypeBinding(typeof(FullyAccessibleClass), Accessibility.Private))   // This class will be fully accessible
    .Allow(new TypeBinding(typeof(InheritableAccessibleClass), Accessibility.Protected))   // Public/protected members only
    .Allow(new TypeBinding(typeof(PublicAccessibleClass), Accessibility.Public))   // Public members only
    .Allow(new TypeBinding(typeof(PartiallyAccessibleClass), Accessibility.None)   // Only the following members
        .WithConstructor([], Accessibility.Public)
        .WithField("AllowedStaticField", Accessibility.Public)
        .WithField("AllowedField", Accessibility.Public)
        .WithMethod("InterfaceMethod", Accessibility.Public))
    .Allow(new AssemblyBinding(Assembly.Load("SomeAssembly"), Accessibility.Protected))   // All public/protected members of assembly
    .Build();  // Generate the final policy
```

After a policy has been defined, it can be used with a `CasAssemblyLoader`. Any assemblies created using the loader will be subject to the policy:

```csharp
var loadContext = new CasAssemblyLoader(policy);
var assembly = loadContext.LoadFromAssemblyPath("Newtonsoft.Json.dll");
// The types in assembly can only access external code if it is whitelisted OR if that code was loaded with the same loader
```

### Default sandbox policy

`CasPolicyBuilder` comes with extension methods (most notably `WithDefaultSandbox`) that add members from the C# standard library. These methods are meant to whitelist as much of the C# standard library as possible, excluding any methods that:

- Cause undefined behavior (such as the [`Unsafe`](https://learn.microsoft.com/en-us/dotnet/api/system.runtime.compilerservices.unsafe) class or intrinsics)
- Access the filesystem
- Access the network
- Access other OS-specific resources (such as processes or pipes)
- Allow for loading other code without verification (such as `System.Reflection.Emit`)

The `WithDefaultSandbox` method should provide a sensible default whitelist that ensures any loaded assemblies cannot gain access to the host system. Testing reveals that the `netstandard` version of `Newtonsoft.Json` is able to run with the default sandbox policy, so it should be fairly comprehensive.

### How it works

This library has three main components:

1. **CIL verification** - before a sandboxed assembly is loaded, it is modified using [Mono.Cecil](https://github.com/jbevain/cecil). Some "guard" CIL instructions are inserted at the beginning of each method in the assembly. When such a method is called for the first time, the guard instructions cause [a fork of Microsoft's ILVerify](https://github.com/DouglasDwyer/JitIlVerification) to run on the method bytecode. The validity of the bytecode is checked and an error is raised if the bytecode does not conform to the CLR ECMA standard.
2. **Insertion of runtime checks** - next, the bytecode of each method is [scanned with Cecil](https://github.com/DouglasDwyer/CasCore/blob/main/CasCore/CasAssemblyLoader.cs#L161-L182). A runtime check is inserted before any external field access or method call (this includes virtual method calls and the creation of delegates). The check causes an exception to be thrown if the assembly does not have permission to access the member. The runtime checks are designed so that the JIT will compile them out if the member is both accessibly and non-virtual.
3. **Calls to shims** - finally, calls to reflection-related APIs (such as `System.Activator.CreateInstance` or `System.Reflection.MethodInfo.Invoke`) are replaced with [calls to shims](/CasCore/MethodShims.cs). These shims perform a runtime check to ensure that the reflected member is accessible. If not, an exception is thrown.

### Testing

Included in this repository are a set of [unit tests](/CasCore.Tests/) to demonstrate the system's functionality. The unit tests show that things like causing memory unsafety or accessing the file system are impossible under the default sandbox policy. Contributions and pull requests to test/address any remaining security vulnerabilities are appreciated.