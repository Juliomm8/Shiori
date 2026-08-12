namespace Shiori.ArchitectureTests.Model;

internal sealed record CompiledTypeReference(
    string TypeFullName,
    string ScopeAssemblyName,
    string Owner);

internal sealed record CompiledMethodTypeUsage(
    string MethodFullName,
    string TypeFullName,
    string ScopeAssemblyName);

internal sealed record CompiledAssemblySnapshot(
    string ProjectName,
    string AssemblyName,
    string AssemblyPath,
    IReadOnlySet<string> AssemblyReferences,
    IReadOnlyList<CompiledTypeReference> TypeReferences);