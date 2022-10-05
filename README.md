# Mono.TextTemplating

[![Build](https://github.com/mono/t4/actions/workflows/build.yml/badge.svg)](https://github.com/mono/t4/actions/workflows/build.yml) [![NuGet version (dotnet-t4)](https://img.shields.io/nuget/v/dotnet-t4.svg?style=flat-square)](https://www.nuget.org/packages/dotnet-t4)

T4 templates are a simple general-purpose way to use C# to generate any kind of text or code files.

[`Mono.TextTemplating`](https://github.com/mono/t4) started out as an open-source reimplementation of the Visual Studio T4 text templating engine, but has since evolved to have many improvements over the original, including support for C# 10 and .NET 6.

The [dotnet-t4](https://www.nuget.org/packages/dotnet-t4/) tool  can be used either to process T4 templates directly, or preprocess them into runtime template classes that can be included in your app and processed at runtime.

```bash
$ dotnet tool install -g dotnet-t4
$ echo "<#@ parameter name='Name' #>Hello <#=Name#>" | t4 -o - -p:Name=World
Hello World
```

To learn more, see the [dotnet-t4 readme](dotnet-t4/readme.md).

For advanced use, the engine itself is available as a package called [Mono.TextTemplating](https://www.nuget.org/packages/Mono.TextTemplating) that can be embedded in an app. For details, see the [Mono.TextTemplating package readme](Mono.TextTemplating/readme.md).
