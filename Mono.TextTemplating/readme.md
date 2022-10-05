
# Mono.TextTemplating

> **NOTE:** To use a template at runtime in your app, you do not need to host the engine. It is generally preferable to use [`dotnet-t4`](https://www.nuget.org/packages/dotnet-t4) to generate a [runtime template class](https://learn.microsoft.com/en-us/visualstudio/modeling/run-time-text-generation-with-t4-text-templates?view=vs-2022&tabs=csharp) and compile that into your app, as this has substantially less overhead than hosting the engine.

[Mono.TextTemplating](https://github.com/mono/t4) is an open-source reimplementation of the Visual Studio T4 text templating engine, and supports C# 10 and .NET 6. This package is the engine package, which can be used to host the T4 engine in your own app.

By default the engine uses the C# compiler from the .NET SDK, but the [`Mono.TextTemplating.Roslyn`](https://www.nuget.org/packages/Mono.TextTemplating.Roslyn) package can be used to bundle a copy of the Roslyn C# compiler and host it in-process. This may improve template compilation performance when compiling multiple templates, and guarantees a specific version of the compiler.

## Usage

This will read a template from `templateFile`, compile and process it, and write the output to `outputFile`:

```csharp
var generator = new TemplateGenerator ();
bool success = await generator.ProcessTemplateAsync (templateFilename, outputFilename);
```

This does the same thing as a series of lower-level steps, allowing it to provide additional compiler arguments by modifying the `TemplateSettings`:

```csharp
string templateContent = File.ReadAllText (templateFilename);

var generator = new TemplateGenerator ();
ParsedTemplate parsed = generator.ParseTemplate (templateFilename, templateContent);
TemplateSettings settings = TemplatingEngine.GetSettings (generator, parsed);

settings.CompilerOptions = "-nullable:enable";

(string generatedFilename, string generatedContent) = await generator.ProcessTemplateAsync (
    parsed, inputFilename, inputContent, outputFilename, settings
);

File.WriteAllText (generatedFilename, generatedContent);
```

## API Overview

### Hosting

In most cases, you need only use or subclass `TemplateGenerator`:
* It implements `ITextTemplatingEngineHost` and `ITextTemplatingSessionHost` with a default implementation that can be overridden if needed.
* It wraps a `TemplateEngine` instance and provides simplified `ProcessTemplateAsync()` and `PreprocessTemplateAsync()` methods.

### VS T4 Compatibility

`Mono.TextTemplating` has session, host and directive processor interfaces and classes in the `Microsoft.VisualStudio.TextTemplating` namespace that are near-identical to the original Visual Studio T4 implementation. This allows older T4 templates and directive processors to work with `Mono.TextTemplating` with few (if any) changes.

* [`ITextTemplatingEngineHost`](https://learn.microsoft.com/en-us/dotnet/api/microsoft.visualstudio.texttemplating.itexttemplatingenginehost?view=visualstudiosdk-2022)
* [`ITextTemplatingSessionHost`](https://learn.microsoft.com/en-us/dotnet/api/microsoft.visualstudio.texttemplating.itexttemplatingsessionhost?view=visualstudiosdk-2022), [`ITextTemplatingSession`](https://learn.microsoft.com/en-us/dotnet/api/microsoft.visualstudio.texttemplating.itexttemplatingsession?view=visualstudiosdk-2022), [`TextTemplatingSession`](https://learn.microsoft.com/en-us/dotnet/api/microsoft.visualstudio.texttemplating.texttemplatingsession?view=visualstudiosdk-2022)
* [`IDirectiveProcessor`](https://learn.microsoft.com/en-us/dotnet/api/microsoft.visualstudio.texttemplating.idirectiveprocessor?view=visualstudiosdk-2022), [`IRecognizeHostSpecific`](https://learn.microsoft.com/en-us/dotnet/api/microsoft.visualstudio.texttemplating.irecognizehostspecific?view=visualstudiosdk-2022), [`DirectiveProcessor`](https://learn.microsoft.com/en-us/dotnet/api/microsoft.visualstudio.texttemplating.directiveprocessor?view=visualstudiosdk-2022), [`DirectiveProcessorException`](https://learn.microsoft.com/en-us/dotnet/api/microsoft.visualstudio.texttemplating.directiveprocessorexception?view=visualstudiosdk-2022), [`RequiresProvidesDirectiveProcessor`](https://learn.microsoft.com/en-us/dotnet/api/microsoft.visualstudio.texttemplating.requiresprovidesdirectiveprocessor?view=visualstudiosdk-2022), [`ParameterDirectiveProcessor`](https://learn.microsoft.com/en-us/dotnet/api/microsoft.visualstudio.texttemplating.parameterdirectiveprocessor?view=visualstudiosdk-2022)

The `Microsoft.VisualStudio.TextTemplating.(ITextTemplatingEngine,Engine)` hosting API is supported but deprecated.

### Advanced

For advanced use, some lower level classes and methods are accessible:

* `TemplatingEngine`: generates C# classes from T4 templates and compiles them into assemblies
* `TemplateGenerator.ParseTemplate()`: uses a `Tokenizer` to parse a template string into a `ParsedTemplate`
* `Tokenizer`: tokenizes an T4 input stream
* `ParsedTemplate`: provides direct access to the segments and directives of a parsed template
* `TemplatingEngine.GetSettings()`: uses the directives in a `ParsedTemplate` to initialize a `TemplateSettings`
* `TemplateSettings`: settings that control code generation and compilation.
* `CompiledTemplate`: a template that has been compiled but not executed

## Differences from VS T4

The `Mono.TextTemplating` engine contains many improvements over the original Visual Studio T4 implementation, including:

* It supports the latest .NET APIs and C# language version
* The engine and the code it generates are compatible with .NET Core and .NET 5+
* It executes templates in an `AssemblyLoadContext`, which allows the generated assemblies to be garbage collected (where supported)
* Parameter directives may use primitive types: `<#@ parameter name="Foo" type="int" #>`
