# Samples

All maintained samples target .NET 10 and reference the local RazorLight project.

## Entity Framework project

This console sample stores templates in an EF Core in-memory database and renders a template with a
layout:

```shell
dotnet run --project samples/RazorLight.Samples/Samples.EntityFrameworkProject.csproj --configuration Release
```

CI runs this sample and verifies its rendered output completes successfully.

## Azure Functions isolated worker

The Azure Functions v4 sample renders a copied `wwwroot/Index.cshtml` template from an HTTP-triggered
isolated-worker function:

```shell
dotnet build samples/FunctionApp-WebMvc-Sample/FunctionApp-WebMvc-Sample/FunctionApp-WebMvc-Sample.csproj --configuration Release
```

CI build-validates this project. Running it requires the Azure Functions Core Tools and the usual
local Functions configuration; it is not started as part of the repository test suite.
