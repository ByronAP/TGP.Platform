# Project Rules

1.  **NEVER** change `PackageReference Include="TGP.Data"` to a `ProjectReference` in any consuming project (`TGP.Microservices.*`, `TGP.AdminPortal`).
    -   *Reason*: CI/CD pipeline requires the NuGet package. Local project references break the build.
    -   *Workflow*: If `TGP.Data` is modified, assume the dependency will be updated via NuGet in the CI environment. Do not attempt to build consuming projects locally if they depend on the *new* data changes that haven't been published yet.
