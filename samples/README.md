# Samples

Small projects that show how to use TruthWeaver. Copy one as the start of your own project.

Each sample references the library projects in this repository with `ProjectReference`. The packages are not on NuGet
yet. The solution builds and tests the samples, so a change to the library API that breaks a sample fails the build.

## Preview features

A consuming project does not need `EnablePreviewFeatures`. The TruthWeaver packages do not set it, so they carry no
requires-preview-features marker. The samples set no preview property.

The packages target `net11.0`. A consuming project must target `net11.0` or later.

## Build settings

`Directory.Build.props` in this folder applies to every sample project. It sets `IsPackable` to `false` and keeps the
repository analyzers. In a CI build, a warning fails the build.
