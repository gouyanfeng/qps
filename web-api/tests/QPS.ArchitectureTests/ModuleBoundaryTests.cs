using Xunit;

namespace QPS.ArchitectureTests;

/// <summary>
/// 校验模块源码层面的边界：项目引用合法不代表可以访问其他模块的内部实现。
/// </summary>
public class ModuleBoundaryTests
{
    private static string WebApiRoot =>
        Path.GetFullPath(Path.Combine(Path.GetDirectoryName(CurrentFile())!, "..", ".."));

    [Fact]
    public void Shared_ShouldNotReferenceAnyBusinessModule()
    {
        var violations = FindViolations("QPS.Shared", new[]
        {
            "using QPS.System",
            "using QPS.Crm",
            "using QPS.Persistence",
            "using QPS.Host"
        });

        Assert.Empty(violations);
    }

    [Fact]
    public void Crm_ShouldNotAccessSystemInternalImplementations()
    {
        var violations = FindViolations("QPS.Crm", new[]
        {
            "using QPS.System.Application",
            "ISystemDbContext",
            "SystemRoles",
            "SystemPermissions",
            "SystemRolePermissions"
        });

        Assert.Empty(violations);
    }

    [Fact]
    public void Mall_ShouldNotAccessSystemCrmOrPersistenceInternals()
    {
        var violations = FindViolations("QPS.Mall", new[]
        {
            "using QPS.System",
            "using QPS.Crm",
            "using QPS.Persistence",
            "ISystemDbContext",
            "SystemRoles",
            "SystemPermissions"
        });

        Assert.Empty(violations);
    }

    [Fact]
    public void MallFeatures_ShouldNotReferenceOtherFeatureHandlers()
    {
        var assembly = typeof(QPS.Mall.DependencyInjection).Assembly;
        var handlers = assembly.GetTypes()
            .Where(type => type.Name.EndsWith("Handler", StringComparison.Ordinal))
            .Select(type => new { type.Name, Feature = FeatureName(type.Namespace) })
            .Where(item => item.Feature is not null)
            .ToList();
        Assert.NotEmpty(handlers);

        var featuresRoot = Path.Combine(WebApiRoot, "src", "QPS.Mall", "Application", "Features");
        Assert.True(Directory.Exists(featuresRoot), $"未找到特性目录：{featuresRoot}");

        var violations = new List<string>();
        foreach (var featureDir in Directory.EnumerateDirectories(featuresRoot))
        {
            var feature = Path.GetFileName(featureDir);
            var foreignHandlers = handlers
                .Where(item => item.Feature != feature)
                .Select(item => item.Name)
                .Distinct(StringComparer.Ordinal)
                .ToList();

            var files = Directory.EnumerateFiles(featureDir, "*.cs", SearchOption.AllDirectories)
                .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                    && !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"));

            foreach (var file in files)
            {
                var lines = File.ReadAllLines(file);
                for (var index = 0; index < lines.Length; index++)
                {
                    foreach (var handlerName in foreignHandlers)
                    {
                        if (lines[index].Contains(handlerName, StringComparison.Ordinal))
                        {
                            violations.Add($"{Path.GetRelativePath(WebApiRoot, file)}:{index + 1} 跨特性引用 {handlerName}");
                        }
                    }
                }
            }
        }

        Assert.Empty(violations);
    }

    private static string? FeatureName(string? ns)
    {
        const string prefix = "QPS.Mall.Application.Features.";
        if (ns is null || !ns.StartsWith(prefix, StringComparison.Ordinal))
        {
            return null;
        }

        var rest = ns[prefix.Length..];
        var separator = rest.IndexOf('.');
        return separator < 0 ? rest : rest[..separator];
    }

    private static IReadOnlyList<string> FindViolations(string projectName, IReadOnlyList<string> forbiddenTokens)
    {
        var projectRoot = Path.Combine(WebApiRoot, "src", projectName);
        Assert.True(Directory.Exists(projectRoot), $"未找到项目目录：{projectRoot}");

        var violations = new List<string>();
        var files = Directory.EnumerateFiles(projectRoot, "*.cs", SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                && !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"));

        foreach (var file in files)
        {
            var lines = File.ReadAllLines(file);
            for (var index = 0; index < lines.Length; index++)
            {
                foreach (var token in forbiddenTokens)
                {
                    if (lines[index].Contains(token, StringComparison.Ordinal))
                    {
                        violations.Add($"{Path.GetRelativePath(WebApiRoot, file)}:{index + 1} 命中 {token}");
                    }
                }
            }
        }

        return violations;
    }

    private static string CurrentFile([global::System.Runtime.CompilerServices.CallerFilePath] string path = "") => path;
}
