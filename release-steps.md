# Release steps

1. Switch branches to 2.x
2. Merge in commits/branches for the release
3. Update version.json and commit with "Release {version}"
4. Wait for tests to pass in CI

Next, do the release:

```bash
git tag -a v2.0.4 -m "Release 2.0.4"
git push origin v2.0.4
msbuild /p:Configuration=Release /p:PublicRelease=true
rm -r packages
for f in packages/Release/*; do nuget push $f; done
```