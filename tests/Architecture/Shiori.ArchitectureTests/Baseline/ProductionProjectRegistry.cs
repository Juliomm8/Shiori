namespace Shiori.ArchitectureTests.Baseline;

internal sealed record ProductionProject(
    string Name,
    string RelativePath);

internal static class ProductionProjectRegistry
{
    public const int ExpectedProjectCount = 13;

    public static IReadOnlyList<ProductionProject> Projects { get; } =
    [
        new(
            "Shiori.Gateway",
            "src/Gateway/Shiori.Gateway/Shiori.Gateway.csproj"),

        new(
            "Shiori.Identity.Api",
            "src/Services/Identity/Shiori.Identity.Api/Shiori.Identity.Api.csproj"),
        new(
            "Shiori.Identity.Application",
            "src/Services/Identity/Shiori.Identity.Application/Shiori.Identity.Application.csproj"),
        new(
            "Shiori.Identity.Domain",
            "src/Services/Identity/Shiori.Identity.Domain/Shiori.Identity.Domain.csproj"),
        new(
            "Shiori.Identity.Infrastructure",
            "src/Services/Identity/Shiori.Identity.Infrastructure/Shiori.Identity.Infrastructure.csproj"),

        new(
            "Shiori.Catalog.Api",
            "src/Services/Catalog/Shiori.Catalog.Api/Shiori.Catalog.Api.csproj"),
        new(
            "Shiori.Catalog.Application",
            "src/Services/Catalog/Shiori.Catalog.Application/Shiori.Catalog.Application.csproj"),
        new(
            "Shiori.Catalog.Domain",
            "src/Services/Catalog/Shiori.Catalog.Domain/Shiori.Catalog.Domain.csproj"),
        new(
            "Shiori.Catalog.Infrastructure",
            "src/Services/Catalog/Shiori.Catalog.Infrastructure/Shiori.Catalog.Infrastructure.csproj"),

        new(
            "Shiori.Tracking.Api",
            "src/Services/Tracking/Shiori.Tracking.Api/Shiori.Tracking.Api.csproj"),
        new(
            "Shiori.Tracking.Application",
            "src/Services/Tracking/Shiori.Tracking.Application/Shiori.Tracking.Application.csproj"),
        new(
            "Shiori.Tracking.Domain",
            "src/Services/Tracking/Shiori.Tracking.Domain/Shiori.Tracking.Domain.csproj"),
        new(
            "Shiori.Tracking.Infrastructure",
            "src/Services/Tracking/Shiori.Tracking.Infrastructure/Shiori.Tracking.Infrastructure.csproj")
    ];
}