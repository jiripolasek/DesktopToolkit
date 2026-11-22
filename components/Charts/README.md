# Charts

Build WinUI dashboards with charts, live graphs, meters, and gauges.

See the [package guide](src/README.md) for requirements, installation, and usage.

## Examples

Open **Charts** in the Desktop Toolkit Gallery. The standalone app in [dev/](dev/) uses the same [sample pages](samples/).
Run it from the repository root.

```powershell
dotnet run --project components/Charts/dev -p:Platform=x64
```

## Development

- [src/](src/) contains the chart controls, templates, and API documentation.
- [tests/](tests/) contains smoke checks and a standalone package consumer.
- [Test-Package.ps1](scripts/Test-Package.ps1) runs the chart smoke suite against the packed library and exercises
  a .NET 8 consumer. Its default `Aot` mode also publishes and runs .NET 10 and .NET 11 Native AOT consumers.

See the [toolkit development guide](../../eng/README.md) for build prerequisites and shared commands. Use `-TestPackage` to run the package checks.
