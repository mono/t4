# dotnet-t4

`dotnet-t4` is a command-line tool for processing T4 templates, a general-purpose way to generate text or code files using C#.

It's part of [Mono.TextTemplating](https://github.com/mono/t4), a modern open-source reimplementation of the Visual Studio T4 text templating engine.

## Example

A T4 template file contains text interleaved with C# or VB.NET code blocks, which is used to generate a template class, then optionally compiled and executed to generate textual output.

Here is an example T4 template, `powers.tt`. It generates a Markdown table of squares and cubes for numbers up to the value specified by the parameter `Max`.

```t4
<#@ output extension=".md" #>
<#@ parameter name="Max" type="int" #>
<#@ import namespace="System.Linq" #>
# Table of Powers
Number | Square | Cube
--- | ---
<# foreach(int i in Enumerable.Range(2,Max)) {#>
<#= i #> | <#= i*i #> | <#= i*i*i #>
<#}#>
```

It can be executed by running `t4 powers.tt -p:Max=6`, which produces the following `powers.md` markdown file:

```md
# Table of Powers
Number | Square | Cube
--- | ---
2 | 4 | 8
3 | 9 | 27
4 | 16 | 64
5 | 25 | 125
6 | 36 | 216
```

Alternatively, invoking `t4 powers.tt -c MyApp.Powers` will produce a `powers.cs` file containing the runtime template class, which you can compile into your app and execute at runtime with new parameter values:

```csharp
string powersTableMarkdown = new MyApp.Powers { Max = 10 }.Process();
```

To learn more about the T4 language, see the [Visual Studio T4 documentation](https://learn.microsoft.com/en-us/visualstudio/modeling/writing-a-t4-text-template?view=vs-2022).

## Usage

`t4` is a CLI tool and may be invoked as follows:

```bash
t4 [options] [template-file]
```

The `template-file` argument is required unless the template text is piped in via `stdin`.

Option | Description
---|---
`-o, --out=<file>` | Set the name or path of the output `<file>`. It defaults to the input filename with its extension changed to `.txt`, or to match the generated code when preprocessing, and may be overridden by template settings. Use `-` instead of a filename to write to stdout.
`-r=<assembly>` | Add an `<assembly>` reference by path or assembly name. It will be resolved from the framework and assembly directories.
`-u`<br/> `--using=<namespace>` | Import a `<namespace>` by generating a using statement.
`-I=<directory>` | Add a `<directory>` to be searched when resolving included files
`-P=<directory>` | Add a `<directory>` to be searched when resolving assemblies.
`-c`<br/>`--class=<name>` | Preprocess the template into class `<name>` for use as a runtime template. The class name may include a namespace.
`-l`<br/>`--useRelativeLinePragmas` | Use relative paths in line pragmas.
`-p`, `--parameter=<name>=<value>` |  Set session parameter `<name>` to `<value>`. The value is accessed from the template's `Session` dictionary, or from a property declared with a parameter directive: `<#@ parameter name='<name>' type='<type>' #>.` <br/> If the `<name>` matches a parameter with a non-string type, the `<value>` will be converted to that type.
`--debug` | Generate debug symbols and keep temporary files.
`-v` <br/> `--verbose` | Output additional diagnostic information to stdout.
`-h`, `-?`, `--help`  | Show help
`--dp=<directive>!<class>!<assembly>` | Set `<directive>` to be handled by directive processor `<class>` in `<assembly>`.
`-a=<processor>!<directive>!<name>!<value>` | Set host parameter `<name>` to `<value>`. It may optionally be scoped to a `<directive>` and/or `<processor>`. The value is accessed from the host's `ResolveParameterValue()` method or from a property declared with a parameter directive: `<#@ parameter name='<name>' #>`.

## Differences from VS T4

The `Mono.TextTemplating` engine contains many improvements over the original Visual Studio T4 implementation, including:

* It supports the latest .NET APIs and C# language version
* The engine and the code it generates are compatible with .NET Core and .NET 5+
* Parameter directives may use primitive types: `<#@ parameter name="Foo" type="int" #>`
* Parameter values passed on the CLI will be automatically converted to the type specified in the parameter directive.
* The CLI can read templates from standard input and output to standard output

Several of these features are demonstrated in the following `bash` one-liner:

```bash
$  echo '<#@ parameter name="Date" type="System.DateTime" #>That was a <#=$"{Date:dddd}"#>' | t4 -o - -p:Date="2016/3/8"
That was a Tuesday
```
