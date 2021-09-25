# PrimeView
![CI](https://github.com/rbergen/PrimeView/actions/workflows/azure-static-web-apps-agreeable-mud-0b27ba210.yml/badge.svg)
![CI](https://github.com/PlummersSoftwareLLC/PrimeView/actions/workflows/github-pages.yml/badge.svg)


This is a Blazor WebAssembly static in-browser web application to view benchmark reports generated in/for the [Primes](https://github.com/PlummersSoftwareLLC/Primes) project.

At the moment, the application loads benchmark reports in JSON format, either from a configured location or using a default approach. More information on how this works can be found in [JsonFileReader/README.md](src/JsonFileReader/README.md).

The supported JSON format is the one generated by the benchmark tool in the Primes repository, when the FORMATTER=json variable is used, with one optional extension: in the metadata object, a string `"user"` property can be added to indicate the user who generated the report.

As the report reader back-end is isolated from the front-end (and added via dependency injection), it will be easy to replace it with a client for a different report provider, like a (REST) API that publishes benchmark reports, once that's available.

## Building

The solution can be built by running the following command from the repository root directory, once [.NET 5.0](https://dotnet.microsoft.com/download/dotnet/5.0) is installed:

```
dotnet publish
```

At the end of the build process, the location of the build output will be indicated in the following line:
```
Frontend -> <repo root>\src\Frontend\bin\Debug\net5.0\publish\
```

## Implementation notes
Where applicable, implementation notes can be found in README.md files in the directories for the respective C#/Blazor projects.

## Attribution

* The source code that gets and sets query string parameters is based on [a blog post](https://www.meziantou.net/bind-parameters-from-the-query-string-in-blazor.htm) by G&eacute;rald Barr&eacute;.
* Local storage is implemented using [Blazored LocalStorage](https://github.com/Blazored/LocalStorage).
* The tables of report summaries and report results are implemented using [BlazorTable](https://github.com/IvanJosipovic/BlazorTable).
* The checkered flag in favicon.ico was made by [Freepik](https://www.freepik.com) from [www.flaticon.com](https://www.flaticon.com/).
