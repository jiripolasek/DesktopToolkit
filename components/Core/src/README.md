# Core

Core provides the shared localization configuration for Desktop Toolkit components. It targets .NET 8 and requires only the .NET runtime.

Implement `IStringResourceLoader` and assign it to `ToolkitLocalization.StringResourceLoader` to supply strings from another source. Each assignment invalidates cached lookups. Missing or blank entries and lookup errors fall through to the standard RESW lookup. Repeated failures for a key are retried after a short delay. Set the loader to null to use the built-in sources alone.

Call `ToolkitLocalization.InvalidateStringResources()` when the app language or the contents of a custom loader change and the next lookup needs to reflect that change.

See the [localization guide](https://desktoptoolkit.jiripolasek.com/localization/) for the catalog names, an example, and the lookup order.
