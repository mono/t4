
# Mono.TextTemplating.Roslyn

> **NOTE:** To use a template at runtime in your app, you do not need to host the engine. It is generally preferable to use [`dotnet-t4`](https://www.nuget.org/packages/dotnet-t4) to generate a [runtime template class](https://learn.microsoft.com/en-us/visualstudio/modeling/run-time-text-generation-with-t4-text-templates?view=vs-2022&tabs=csharp) and compile that into your app, as this has substantially less overhead than hosting the engine.

The package allows apps that host the [`Mono.TextTemplating`](https://www.nuget.org/packages/Mono.TextTemplating) T4 engine to bundle a copy of the Roslyn C# compiler and use it in-process. This may improve template compilation performance when compiling multiple templates, and guarantees a specific version of the compiler.

In addition to installing the package, you must call the `UseInProcessCompiler()` extension method on your `TemplateEngine` instance:

```csharp
var engine = new TemplatingEngine();
engine.UseInProcessCompiler();
```

When using the in-process compiler, the T4 engine will not write any intermediate files to disk. Generated C# files and compiled assemblies will be kept in memory.
