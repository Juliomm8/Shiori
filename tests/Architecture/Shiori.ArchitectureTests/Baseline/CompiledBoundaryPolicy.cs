using Shiori.ArchitectureTests.Model;

namespace Shiori.ArchitectureTests.Baseline;

internal static class CompiledBoundaryPolicy
{
    public static bool IsDomainOrApplicationProject(
        string projectName)
    {
        return projectName.EndsWith(
                   ".Domain",
                   StringComparison.Ordinal) ||
               projectName.EndsWith(
                   ".Application",
                   StringComparison.Ordinal);
    }

    public static bool IsApplicationProject(
        string projectName)
    {
        return projectName.EndsWith(
            ".Application",
            StringComparison.Ordinal);
    }

    public static bool IsApiProject(
        string projectName)
    {
        return projectName.EndsWith(
            ".Api",
            StringComparison.Ordinal);
    }

    public static string GetOwnDomainAssembly(
        string apiProjectName)
    {
        return ReplaceLayerSuffix(
            apiProjectName,
            ".Api",
            ".Domain");
    }

    public static string GetOwnInfrastructureAssembly(
        string apiProjectName)
    {
        return ReplaceLayerSuffix(
            apiProjectName,
            ".Api",
            ".Infrastructure");
    }

    public static string?
        FindDomainOrApplicationViolation(
            CompiledTypeReference reference)
    {
        var typeName =
            reference.TypeFullName;

        if (typeName.StartsWith(
                "Microsoft.EntityFrameworkCore.",
                StringComparison.Ordinal))
        {
            return "Persistence / Entity Framework Core";
        }

        if (typeName.StartsWith(
                "MongoDB.",
                StringComparison.Ordinal))
        {
            return "Persistence / MongoDB";
        }

        if (typeName.StartsWith(
                "Npgsql.",
                StringComparison.Ordinal))
        {
            return "Persistence / PostgreSQL";
        }

        if (typeName.StartsWith(
                "RabbitMQ.Client.",
                StringComparison.Ordinal))
        {
            return "Broker / RabbitMQ";
        }

        if (typeName.StartsWith(
                "Yarp.",
                StringComparison.Ordinal))
        {
            return "Gateway / YARP";
        }

        if (typeName.StartsWith(
                "OpenIddict.",
                StringComparison.Ordinal))
        {
            return "Identity infrastructure / OpenIddict";
        }

        if (typeName.StartsWith(
                "Microsoft.AspNetCore.",
                StringComparison.Ordinal) ||
            typeName.StartsWith(
                "Microsoft.Net.Http.Headers.",
                StringComparison.Ordinal))
        {
            return "HTTP transport / ASP.NET Core";
        }

        if (typeName.StartsWith(
                "System.Net.Http.",
                StringComparison.Ordinal))
        {
            return "HTTP transport";
        }

        if (typeName.Equals(
                "System.Security.Claims.ClaimsPrincipal",
                StringComparison.Ordinal))
        {
            return "HTTP/authentication transport principal";
        }

        if (typeName.Equals(
                "System.Linq.IQueryable`1",
                StringComparison.Ordinal))
        {
            return "Persistence query provider";
        }

        if (typeName.StartsWith(
                "System.Data.Common.Db",
                StringComparison.Ordinal) ||
            typeName.Equals(
                "System.Data.IDbConnection",
                StringComparison.Ordinal) ||
            typeName.Equals(
                "System.Data.IDbTransaction",
                StringComparison.Ordinal))
        {
            return "Database implementation abstraction";
        }

        if (IsExternalProviderType(reference))
        {
            return "External provider adapter/model";
        }

        return null;
    }

    private static bool IsExternalProviderType(
        CompiledTypeReference reference)
    {
        return StartsWithProviderName(
                   reference.ScopeAssemblyName,
                   "AniList") ||
               StartsWithProviderName(
                   reference.ScopeAssemblyName,
                   "MangaDex") ||
               StartsWithProviderName(
                   reference.TypeFullName,
                   "AniList") ||
               StartsWithProviderName(
                   reference.TypeFullName,
                   "MangaDex");
    }

    private static bool StartsWithProviderName(
        string value,
        string provider)
    {
        return value.StartsWith(
            provider,
            StringComparison.OrdinalIgnoreCase);
    }

    private static string ReplaceLayerSuffix(
        string projectName,
        string currentSuffix,
        string targetSuffix)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            projectName);

        if (!projectName.EndsWith(
                currentSuffix,
                StringComparison.Ordinal))
        {
            throw new ArgumentException(
                $"Project '{projectName}' does not end with " +
                $"expected suffix '{currentSuffix}'.",
                nameof(projectName));
        }

        return projectName[
                   ..^currentSuffix.Length] +
               targetSuffix;
    }
}