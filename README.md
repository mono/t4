# Mono.TextTemplating

[![Build](https://github.com/mono/t4/actions/workflows/build.yml/badge.svg)](https://github.com/mono/t4/actions/workflows/build.yml) [![Gitter](https://badges.gitter.im/mono/t4.svg)](https://gitter.im/mono/t4?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge)

Mono.TextTemplating is an open-source implementation of the T4 text templating engine, a simple general-purpose way to use C# to generate any kind of text files.

It's provided as a `dotnet` tool called `t4`:

```bash
$ dotnet tool install -g dotnet-t4
$ echo "<#@ parameter name='Name' #>Hello <#=Name#>" | t4 -o - -p:Name=World
Hello World
```

You can use the `-c <classname>` option to convert a T4 template into a C# class that can be compiled into your app and executed at runtime. For help on other options, use the `-h` argument.

To learn more about the T4 language, see the [Visual Studio T4 documentation]( https://docs.microsoft.com/en-us/visualstudio/modeling/code-generation-and-t4-text-templates?view=vs-2017).

For more advanced use cases, the engine itself is also available as a library called called `Mono.TextTemplating` that can be integrated into any .NET 4.5+ or .NET Standard 2.0 app.

## NuGet Packages

Package | Description
--- | ---
[dotnet-t4](https://www.nuget.org/packages/dotnet-t4/) | T4 command-line tool for executing .tt files and generating runtime template classes
[Mono.TextTemplating](https://www.nuget.org/packages/Mono.TextTemplating) | T4 engine for custom hosting
[Mono.TextTemplating.Roslyn](https://www.nuget.org/packages/Mono.TextTemplating.Roslyn) | Optional in-process compiler for Mono.TextTemplating hosts
[dotnet-t4-project-tool](https://www.nuget.org/packages/dotnet-t4-project-tool/) | `DotNetCliToolReference` version of `dotnet-t4` (deprecated)

CI builds are published to [GitHub Packages](https://github.com/orgs/mono/packages?repo_name=t4).
