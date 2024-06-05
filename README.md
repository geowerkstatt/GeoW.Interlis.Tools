[![CI](https://github.com/GeoWerkstatt/GeoW.Interlis.Tools/actions/workflows/ci.yml/badge.svg)](https://github.com/GeoWerkstatt/GeoW.Interlis.Tools/actions/workflows/ci.yml)

# geowerkstatt Interlis Tools

## GitHub NuGet Feed
NuGet package: https://github.com/GeoWerkstatt/GeoW.Interlis.Tools/pkgs/nuget/Geowerkstatt.Interlis.Tools.Compiler

To authenticate to the geowerkstatt GitHub Packages registry you must use a [personal access token (classic)](https://docs.github.com/en/authentication/keeping-your-account-and-data-secure/managing-your-personal-access-tokens#creating-a-personal-access-token-classic) with at least `read:packages` scope to install packages associated with other private repositories.
Then create a _nuget.config_ file in your project directory specifying GitHub Packages as a source (see example below).
You must replace:

* `USERNAME` with the name of your personal account on GitHub.
* `PAT_CLASSIC` with your personal access token (classic).
```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" protocolVersion="3" />
    <add key="github" value="https://nuget.pkg.github.com/GeoWerkstatt/index.json" protocolVersion="3" />
  </packageSources>
  <packageSourceCredentials>
    <github>
      <add key="Username" value="USERNAME" />
      <add key="ClearTextPassword" value="PAT_CLASSIC" />
    </github>
  </packageSourceCredentials>
</configuration>
```

The sample file above is a minimal configuration and also located in the root of this repository.
