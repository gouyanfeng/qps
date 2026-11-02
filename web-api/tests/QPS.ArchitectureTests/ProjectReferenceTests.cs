using System.Xml.Linq;
using Xunit;

namespace QPS.ArchitectureTests;

/// <summary>
/// 校验项目引用方向，防止模块之间出现反向依赖或循环依赖。
/// </summary>
public class ProjectReferenceTests
{
    private static string WebApiRoot =>
        Path.GetFullPath(Path.Combine(Path.GetDirectoryName(CurrentFile())!, "..", ".."));

    [Fact]
    public void Shared_ShouldNotReferenceAnyModuleOrHost()
    {
        var references = GetProjectReferences("QPS.Shared");

        Assert.DoesNotContain("QPS.System", references);
        Assert.DoesNotContain("QPS.Crm", references);
        Assert.DoesNotContain("QPS.Mall", references);
        Assert.DoesNotContain("QPS.Persistence", references);
        Assert.DoesNotContain("QPS.Host", references);
    }

    [Fact]
    public void System_ShouldNotReferenceCrmMallPersistenceOrHost()
    {
        var references = GetProjectReferences("QPS.System");

        Assert.DoesNotContain("QPS.Crm", references);
        Assert.DoesNotContain("QPS.Mall", references);
        Assert.DoesNotContain("QPS.Persistence", references);
        Assert.DoesNotContain("QPS.Host", references);
    }

    [Fact]
    public void Crm_ShouldNotReferenceMallPersistenceOrHost()
    {
        var references = GetProjectReferences("QPS.Crm");

        Assert.DoesNotContain("QPS.Mall", references);
        Assert.DoesNotContain("QPS.Persistence", references);
        Assert.DoesNotContain("QPS.Host", references);
    }

    [Fact]
    public void Mall_ShouldNotReferenceOtherModulesPersistenceOrHost()
    {
        var references = GetProjectReferences("QPS.Mall");

        Assert.DoesNotContain("QPS.System", references);
        Assert.DoesNotContain("QPS.Crm", references);
        Assert.DoesNotContain("QPS.Persistence", references);
        Assert.DoesNotContain("QPS.Host", references);
    }

    [Fact]
    public void Persistence_ShouldNotReferenceHost()
    {
        var references = GetProjectReferences("QPS.Persistence");

        Assert.DoesNotContain("QPS.Host", references);
    }

    [Fact]
    public void Projects_ShouldNotHaveCircularReferences()
    {
        var projects = new[]
        {
            "QPS.Shared",
            "QPS.System",
            "QPS.Crm",
            "QPS.Mall",
            "QPS.Persistence",
            "QPS.Host"
        };

        var graph = projects.ToDictionary(
            project => project,
            project => GetProjectReferences(project));

        foreach (var project in projects)
        {
            var visited = new HashSet<string>(StringComparer.Ordinal);
            var stack = new Stack<string>(graph[project]);
            while (stack.Count > 0)
            {
                var current = stack.Pop();
                if (current == project)
                {
                    Assert.Fail($"检测到循环项目引用：{project} 直接或间接引用了自身。");
                }

                if (!visited.Add(current) || !graph.TryGetValue(current, out var next))
                {
                    continue;
                }

                foreach (var reference in next)
                {
                    stack.Push(reference);
                }
            }
        }
    }

    private static IReadOnlyList<string> GetProjectReferences(string projectName)
    {
        var csprojPath = Path.Combine(WebApiRoot, "src", projectName, $"{projectName}.csproj");
        Assert.True(File.Exists(csprojPath), $"未找到项目文件：{csprojPath}");

        return XDocument.Load(csprojPath)
            .Descendants("ProjectReference")
            .Select(element => Path.GetFileNameWithoutExtension((string?)element.Attribute("Include") ?? string.Empty))
            .Where(name => !string.IsNullOrEmpty(name))
            .ToList();
    }

    private static string CurrentFile([global::System.Runtime.CompilerServices.CallerFilePath] string path = "") => path;
}
