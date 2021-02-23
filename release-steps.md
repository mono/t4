# Release steps

Use https://github.com/dotnet/Nerdbank.GitVersioning/blob/master/doc/nbgv-cli.md

To create release branch in master:

```
nbgv prepare-release
nbgv tag
```

And push branches and tags.

To make a servicing release:

* Bump version.json
* `nbgv tag`

And push branches and tags.
