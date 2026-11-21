# JPSoftworks Desktop Toolkit documentation

This branch contains the Astro Starlight site. Component guides are generated from the package metadata and NuGet READMEs on main.
Use Node 24 or later and PowerShell 7. In the documentation worktree, run:

```sh
npm ci
npm run dev
npm run build
```

The generator finds the main worktree through Git. With two separate clones, set `COMPONENTS_ROOT` to the absolute path of
the library checkout. To open the docs worktree from a clone of main, run `git worktree add ../DesktopToolkit-docs docs`.

To preview the API reference, use Windows with the .NET SDK specified in the library checkout's `global.json`, the .NET 10
runtime, and Windows SDK 10.0.19041.0. Set the path to the library checkout in PowerShell:

```powershell
$env:COMPONENTS_ROOT = '../DesktopToolkit'
npm run build:api
npm run dev
```

The API reference uses DefaultDocumentation to combine the compiled assemblies with their XML comments. It is generated
in one build per library using the first target framework listed in the project. The Pages workflow builds the reference
and the Starlight site on changes to either branch and publishes the static output to the `github-pages` environment.
