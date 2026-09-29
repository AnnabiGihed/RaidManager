---
name: raidmanager-project-packaging
description: Maintain RaidManager .csproj package metadata when creating projects or changing their packaging, version, or descriptions.
---

# RaidManager project packaging

Every `.csproj` has a project-specific `PropertyGroup` with `GeneratePackageOnBuild`, `PackageId`, `Version`, `PackageTags`, and `Description`. Use `RaidManager.<ProjectName>` as the package ID and keep descriptions factual to the project's current responsibility. Keep `Version` aligned across projects unless a release decision explicitly changes it.

Set `GeneratePackageOnBuild` to `true` for reusable source libraries. Runnable API, web, worker, and Aspire hosts and all test projects are not packages: set `GeneratePackageOnBuild` and `IsPackable` to `false` for them. Keep `IsTestProject` on test projects.

Shared author, repository, and license metadata lives in `Directory.Build.props`. The author is Gihed Annabi. The package license must match the repository's `LICENSE` file; do not declare an SPDX license that contradicts it. Keep dependency versions in `Directory.Packages.props`, not in individual `PackageReference` elements.

After changing project metadata, verify the evaluated properties with `dotnet msbuild <project.csproj> -getProperty:PackageId,Version,GeneratePackageOnBuild,Description` and build the solution. When changing library packaging, inspect a generated `.nupkg` and run the unit tests.
