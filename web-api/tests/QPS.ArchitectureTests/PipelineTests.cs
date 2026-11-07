using System.Reflection;
using MediatR;
using Xunit;

namespace QPS.ArchitectureTests;

/// <summary>
/// 校验公共 Pipeline 只在 Shared 中实现一套，业务模块不得重复实现。
/// </summary>
public class PipelineTests
{
    [Fact]
    public void BusinessModules_ShouldNotImplementPipelineBehaviours()
    {
        AssertNoPipelineBehaviours(typeof(QPS.System.DependencyInjection).Assembly);
        AssertNoPipelineBehaviours(typeof(QPS.Crm.DependencyInjection).Assembly);
        AssertNoPipelineBehaviours(typeof(QPS.Mall.DependencyInjection).Assembly);
        AssertNoPipelineBehaviours(typeof(QPS.Persistence.DependencyInjection).Assembly);
    }

    [Fact]
    public void Shared_ShouldContainTheThreeCommonBehaviours()
    {
        var behaviourTypes = typeof(QPS.Shared.DependencyInjection).Assembly
            .GetTypes()
            .Where(type => type is { IsAbstract: false, IsInterface: false })
            .Where(type => type.GetInterfaces().Any(IsOpenPipelineBehaviour))
            .Select(type => type.Name)
            .ToList();

        Assert.Contains("PermissionBehaviour`2", behaviourTypes);
        Assert.Contains("ValidationBehaviour`2", behaviourTypes);
        Assert.Contains("TransactionBehaviour`2", behaviourTypes);
    }

    private static void AssertNoPipelineBehaviours(Assembly assembly)
    {
        var duplicates = assembly
            .GetTypes()
            .Where(type => type is { IsAbstract: false, IsInterface: false })
            .Where(type => type.GetInterfaces().Any(IsOpenPipelineBehaviour))
            .Select(type => type.FullName)
            .ToList();

        Assert.Empty(duplicates);
    }

    private static bool IsOpenPipelineBehaviour(Type type) =>
        type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IPipelineBehavior<,>);
}
