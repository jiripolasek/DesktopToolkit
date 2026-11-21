# Contributing

Contributions to JPSoftworks Desktop Toolkit are welcome. Please follow the
[code of conduct](CODE_OF_CONDUCT.md) in project discussions and reviews.

## Bugs and feature requests

Choose the affected component in either form. Select **General issue** for a new control, the gallery,
documentation, or a concern that spans components.

Open a [bug report](https://github.com/jiripolasek/DesktopToolkit/issues/new?template=bug_report.yml)
with the steps to reproduce the problem and the expected and actual result. For component bugs,
include the package version, your .NET target framework, Windows App SDK version, Windows build, and architecture.
A small XAML or C# example is especially helpful for control issues. Remove personal information
and credentials from logs before sharing them.

For a [feature request](https://github.com/jiripolasek/DesktopToolkit/issues/new?template=feature_request.yml),
describe the workflow it would improve and give a concrete example. For substantial changes,
discuss the scope with a maintainer before investing in a large implementation.

Report security vulnerabilities privately as described in [SECURITY.md](SECURITY.md).

## Development

The [development guide](eng/README.md) covers prerequisites, repository layout, builds,
control tests, package checks, and running the gallery. Keep a component's package, tests,
samples, and development app together in its component directory.

## Pull requests

Keep a pull request focused on one problem. Explain the problem, the resulting behavior,
and how you checked it. State any validation you did not run, including live UI or package
installation checks when relevant. Include screenshots for visible UI changes and use the
[pull request template](.github/pull_request_template.md).

The project uses the [Apache License 2.0](LICENSE).
