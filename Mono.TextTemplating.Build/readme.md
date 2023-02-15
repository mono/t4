# T4.BuildTools

`T4.BuildTools` is a set of MSBuild tasks and targets for for processing T4 templates, a general-purpose way to generate text or code files using C#.

It's part of [Mono.TextTemplating](https://github.com/mono/t4), a modern open-source reimplementation of the Visual Studio T4 text templating engine.

## Usage

These targets introduce two new MSBuild item types: `T4Transform` and `T4Preprocess`. They are processed automatically during the build and when saving the template file in Visual Studio.

> **NOTE**: These items are processed using the `Mono.TextTemplating` T4 engine and host. Host-specific templates will not have access to the Visual Studio T4 host.

`T4Transform` items are transformed when saving the template in Visual Studio and when explicitly invoking the `TransformTemplate` MSBuild target. The `TransformOnBuild` property can be set to transform the templates on every build by calling this target automatically. The build is incremental by default, so a template will only be transformed if its input files are newer than its output.

`T4Preprocess` items are preprocessed into a class that can be instantiated and executed from project code. The preprocessed class is generated in the intermediate output directory and included in the build automatically, similar to a source generator.

### Customizing the Build

Template transformation can be customized using a range of MSBuild properties, items and metadata.

### Properties

| Property | Description
| -- | --
| `TransformOnBuild` | Set this to `True` to automatically transform `T4Transform` items on build. `T4Preprocess` items are transformed on build regardless of this setting unless legacy preprocessing is enabled.
| `T4DefaultNamespace` | Sets the namespace to be used when generating T4 classes. Defaults to the project's `$(RootNamespace)`.
| `TransformOutOfDateOnly` | Setting this to `False` will disable the incremental build and force all template to be re-transformed.

### Items

<table>
<tr>
<th>Item</th><th>Description</th>
</tr>
<tr><td>

`T4Argument`

</td><td>
Pass a parameter value to the T4 templates, optionally scoped to a directive processor and/or directive. This may take one of several forms:

* Parameter name and `Value` metadata, with optional `Processor` and/or `Directive` metadata
* Encoded `<name=>=<value>` key-value pair
* The `<processor>!<directive>!<name>!<value>` format used by the CLI `t4 -a` option

For example:

```xml
<ItemGroup>
  <T4Argument Include="Greeting" Value="Hello!" />
  <T4Argument Include="Year=2023" />
  <T4Argument Include="MyProcessor!MyDirective!Month!June" />
</ItemGroup>
```

</td></tr><tr><td>

`DirectiveProcessor`

</td><td>

Register a custom directive processor. This may use the same use the `<name>!<class>!<assembly>` format as the CLI `t4 --dp` option, or separate `Class` and `Assembly` metadata.

```xml
<ItemGroup>
  <DirectiveProcessor Include="FirstProcessor" Class="MyProcessors.SecondProcessor" Assembly="MyProcessors.dll" />
  <DirectiveProcessor Include="SecondProcessor!MyProcessors.SecondProcessor!MyProcessors.dll" />
</ItemGroup>
```

</td></tr><tr><td>

`T4ReferencePath`

</td><td>

Adds a search directory for resolving assembly references in `T4Transform` templates. Affects `<#@assembly#>` directives and calls to the host's `ResolveAssemblyReference(...)` method.

</td></tr><tr><td>

`T4IncludePath`

</td><td>

Adds a search directory for resolving `<#@include#>` directives in `T4Transform` and `T4Preprocess` templates. For `T4Transform` items, this also affects calls to the host's `LoadIncludeText(...)` method.

</td></tr><tr><td>

`T4AssemblyReference`

</td><td>

Additional assemblies to be referenced when processing `T4Transform` items. May be a absolute path, or relative to the project or the `T4ReferencePath` directories.

</td></tr></table>

### Item Metadata

The `T4Transform` and `T4Preprocess` items have optional metadata that can be used to control the  path used for the generated output.

| Metadata | Description
| -- | --
|  `OutputFilePath`| Overrides the output folder
|  `OutputFileName`| Overrides the output file name

### CLI Properties

There also are several properties that are intended to be passed in when invoking the target on the CLI and should not be used in project/targets files:

| Property | Description
| -- | --
| `TransformFile` | Semicolon-separated list of template filenames. If `TransformFile` is set when invoking the `TextTransform` target explicitly, then only the templates specified by it will be transformed.
| `T4Arguments` | Semicolon-separated list of `T4Argument` items.

For example:

```bash
dotnet msbuild -t:TransformTemplates -p:TransformFile=Foo.cs -p:T4Arguments="Foo=1;Bar=2"
```

## Target Extensibility

The `TransformTemplatesCore` target is provided as an extension points for custom targets that require running before or after template processing. This target runs when automatic or explicit transformation takes place, whereas the `TransformTemplates` target only runs when invoked explicitly.

The outputs of the transformed and preprocessed templates are available as `GeneratedTemplateOutput` and `PreprocessedTemplateOutput` items respectively, and assemblies referenced by preprocessed templates are available as `T4RequiredAssemblies` items.

 These items are only available after the transform targets have run, so are not available in the MSBuild project at evaluation time. To access them in custom targets, use `AfterTargets="TransformTemplatesCore"` to order your target after template transformation.

For example:

```xml
<Target Name="MyCustomTarget" AfterTargets="TransformTemplatesCore">
  <Message Text="Transformed templates: @(GeneratedTemplates)" />
  <Message Text="Preprocessed templates: @(PreprocessedTemplates)" />
  <Message Text="Preprocessed assemblies: @(T4RequiredAssemblies)" />
</Target>
```

## Compatibility

The following properties, items and metadata are provided for partial backwards compatibility with the Visual Studio [Microsoft.TextTemplating](https://learn.microsoft.com/en-us/visualstudio/modeling/code-generation-in-a-build-process) MSBuild targets.

| Kind | Name | Description
| -- | -- |--
| Item | `T4ParameterValues` | Equivalent to `T4Argument` item
| Property | `UseLegacyT4Preprocessing` | Place preprocessed templates beside the template files instead of dynamically injecting them into the build
| Property | `IncludeFolders` | Equivalent to `T4IncludePath` items
| Property | `PreprocessTemplateDefaultNamespace` | Equivalent to `T4DefaultNamespace` property
| Property | `AfterTransform` | List of targets to run before template transformation. Use `BeforeTargets="TransformTemplatesCore"` instead.
| Property | `AfterTransform` | List of targets to run after template transformation. Use `AfterTargets="TransformTemplatesCore"` instead.
| Metadata | `DirectiveProcessor.Codebase` | Equivalent to `Assembly` metadata