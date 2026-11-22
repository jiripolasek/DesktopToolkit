# Core

Configure string localization across Desktop Toolkit components.

See the [package guide](src/README.md) for requirements and usage.

## Development

- [src/](src/) contains the Core package and shared localization API.
- [tests/](tests/) verifies that previously compiled control assemblies retain friend access to Core.

Core builds check the public API and the internal signatures used by control packages against the `PublicAPI` and `InternalAPI` files in `src/`.
Add new signatures to the corresponding `.Unshipped.txt` file and move them into `.Shipped.txt` at release.
When a new control first ships with friend access, add its assembly name to the compatibility test. Preserve earlier grants so apps can upgrade Core independently of their controls.

See the [toolkit development guide](../../eng/README.md) for build prerequisites and shared commands.
