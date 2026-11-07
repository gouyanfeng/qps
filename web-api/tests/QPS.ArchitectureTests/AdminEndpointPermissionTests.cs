using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using QPS.Shared.Contracts;
using Xunit;
using Xunit.Abstractions;

namespace QPS.ArchitectureTests;

/// <summary>
/// 管理端接口必须声明功能权限：仅靠认证（FallbackPolicy）或角色策略（AdminOnly）只回答“是否登录/是不是管理员”，
/// 不回答“是否具备该功能”。管理端端点要么在 Controller / Handler（请求类型）上标注 [Permission]，
/// 要么命中 AdminPermissionBaseline.txt 中的存量基线；新增端点不在基线里则直接失败。
/// </summary>
public class AdminEndpointPermissionTests
{
    private const string BaselineFileName = "AdminPermissionBaseline.txt";

    private static readonly Assembly[] ModuleAssemblies =
    {
        typeof(QPS.System.DependencyInjection).Assembly,
        typeof(QPS.Crm.DependencyInjection).Assembly,
        typeof(QPS.Mall.DependencyInjection).Assembly
    };

    /// <summary>
    /// 显式豁免清单：键为 "Controller.Method"，值为豁免原因。只允许登录、当前用户自身操作等无法挂功能权限的端点。
    /// 与基线不同，豁免项代表“永远不需要功能权限”，不会随时间补齐。
    /// </summary>
    private static readonly Dictionary<string, string> Exemptions = new(StringComparer.Ordinal)
    {
        ["AuthController.Logout"] = "注销当前会话：只结束登录态，不涉及业务功能",
        ["AuthController.GetUserPermissions"] = "读取当前登录用户自身的权限码，登录后即可调用",
        ["AuthController.ChangePassword"] = "修改当前登录用户自己的密码，只作用于本人"
    };

    private static readonly Lazy<Dictionary<string, Type>> RequestTypesByName = new(BuildRequestTypeMap);

    private readonly ITestOutputHelper _output;

    public AdminEndpointPermissionTests(ITestOutputHelper output)
    {
        _output = output;
    }

    private static string WebApiRoot =>
        Path.GetFullPath(Path.Combine(Path.GetDirectoryName(CurrentFile())!, "..", ".."));

    [Fact]
    public void AdminEndpoints_ShouldDeclarePermissionOrBeInBaseline()
    {
        var baselinePath = Path.Combine(WebApiRoot, "tests", "QPS.ArchitectureTests", BaselineFileName);
        Assert.True(File.Exists(baselinePath), $"未找到管理端权限基线文件：{baselinePath}");
        var baselineKeys = File.ReadLines(baselinePath)
            .Select(line => line.Trim())
            .Where(line => line.Length > 0 && !line.StartsWith("#", StringComparison.Ordinal))
            .ToHashSet(StringComparer.Ordinal);

        var violations = new List<string>();
        var protectedKeys = new HashSet<string>(StringComparer.Ordinal);
        var scannedKeys = new HashSet<string>(StringComparer.Ordinal);

        foreach (var assembly in ModuleAssemblies)
        {
            var controllersRoot = Path.Combine(WebApiRoot, "src", assembly.GetName().Name!, "Controllers");
            foreach (var controller in assembly.GetTypes()
                         .Where(type => type is { IsClass: true, IsPublic: true })
                         .Where(type => type.Name.EndsWith("Controller", StringComparison.Ordinal)))
            {
                var file = FindControllerFile(controllersRoot, controller);
                var controllerSource = file is null ? string.Empty : File.ReadAllText(file);
                var controllerRoutes = controller.GetCustomAttributes<RouteAttribute>(true)
                    .Select(attribute => attribute.Template)
                    .Where(template => !string.IsNullOrWhiteSpace(template))
                    .Select(template => template!.Trim())
                    .ToList();

                foreach (var method in controller.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
                {
                    var httpAttributes = method.GetCustomAttributes<HttpMethodAttribute>(true).ToList();
                    if (httpAttributes.Count == 0)
                    {
                        continue;
                    }

                    var adminRoute = httpAttributes
                        .Select(attribute => CombineRoute(controllerRoutes.FirstOrDefault(), attribute.Template))
                        .FirstOrDefault(IsAdminRoute);
                    if (adminRoute is null)
                    {
                        continue;
                    }

                    if (controller.GetCustomAttribute<AllowAnonymousAttribute>(true) is not null
                        || method.GetCustomAttribute<AllowAnonymousAttribute>(true) is not null)
                    {
                        continue;
                    }

                    var key = $"{controller.Name}.{method.Name}";
                    scannedKeys.Add(key);

                    var fileLine = file is null ? 0 : FindMethodLine(controllerSource, method.Name);
                    var requestTypes = ResolveRequestTypes(method, controllerSource);

                    if (controller.GetCustomAttribute<PermissionAttribute>(true) is not null
                        || method.GetCustomAttribute<PermissionAttribute>(true) is not null
                        || requestTypes.Any(type => type.GetCustomAttribute<PermissionAttribute>() is not null))
                    {
                        protectedKeys.Add(key);
                        continue;
                    }

                    if (requestTypes.Count > 0 && requestTypes.All(IsAnonymousRequest))
                    {
                        continue;
                    }

                    if (Exemptions.ContainsKey(key) || baselineKeys.Contains(key))
                    {
                        continue;
                    }

                    var location = file is null
                        ? controller.FullName ?? controller.Name
                        : $"{Path.GetRelativePath(WebApiRoot, file)}:{fileLine}";
                    var requestNames = requestTypes.Count == 0
                        ? "直接处理（未经过 MediatR）"
                        : string.Join(", ", requestTypes.Select(type => type.Name));
                    violations.Add(
                        $"{location} {key} [{adminRoute}] 缺少 [Permission] 且不在基线；请求类型 {requestNames} 也未标注权限码");
                }
            }
        }

        foreach (var endpoint in FindHostAdminEndpoints())
        {
            scannedKeys.Add(endpoint.Key);
            if (endpoint.HasPermission)
            {
                protectedKeys.Add(endpoint.Key);
                continue;
            }

            if (Exemptions.ContainsKey(endpoint.Key) || baselineKeys.Contains(endpoint.Key))
            {
                continue;
            }

            violations.Add($"{endpoint.Location} {endpoint.Key} [{endpoint.Route}] 缺少 [Permission] 且不在基线");
        }

        ReportBaselineProgress(baselineKeys, protectedKeys, scannedKeys);

        Assert.True(
            violations.Count == 0,
            $"管理端存在 {violations.Count} 个未声明 [Permission] 且不在基线的端点：{Environment.NewLine}"
            + string.Join(Environment.NewLine, violations));
    }

    /// <summary>
    /// 只提示不失败：输出基线中已经补齐权限、以及代码中已消失的条目，便于逐步缩小基线。
    /// </summary>
    private void ReportBaselineProgress(
        IReadOnlySet<string> baselineKeys,
        IReadOnlySet<string> protectedKeys,
        IReadOnlySet<string> scannedKeys)
    {
        var alreadyFixed = baselineKeys.Where(protectedKeys.Contains).OrderBy(key => key).ToList();
        var missing = baselineKeys.Where(key => !scannedKeys.Contains(key)).OrderBy(key => key).ToList();

        _output.WriteLine(
            $"基线共 {baselineKeys.Count} 条；其中 {alreadyFixed.Count} 条已补上 [Permission]，可从基线删除；"
            + $"{missing.Count} 条在代码中已找不到（端点删除或改名），可清理。");

        if (alreadyFixed.Count > 0)
        {
            _output.WriteLine("已补齐：" + string.Join(", ", alreadyFixed));
        }

        if (missing.Count > 0)
        {
            _output.WriteLine("已失效：" + string.Join(", ", missing));
        }
    }

    private static IReadOnlyList<HostAdminEndpoint> FindHostAdminEndpoints()
    {
        var endpoints = new List<HostAdminEndpoint>();
        var hostControllersRoot = Path.Combine(WebApiRoot, "src", "QPS.Host", "Controllers");
        if (!Directory.Exists(hostControllersRoot))
        {
            return endpoints;
        }

        foreach (var file in Directory.EnumerateFiles(hostControllersRoot, "*.cs", SearchOption.AllDirectories))
        {
            var source = File.ReadAllText(file);
            var route = Regex.Match(source, @"\[Route\(""([^""]+)""\)\]");
            if (!route.Success || !IsAdminRoute(route.Groups[1].Value))
            {
                continue;
            }

            if (source.Contains("[AllowAnonymous]", StringComparison.Ordinal))
            {
                continue;
            }

            var hasControllerPermission = source.Contains("[Permission(", StringComparison.Ordinal);
            var controller = Regex.Match(source, @"public\s+class\s+([A-Za-z0-9_]+Controller)");
            var controllerName = controller.Success ? controller.Groups[1].Value : Path.GetFileNameWithoutExtension(file);

            foreach (Match http in Regex.Matches(source, @"\[Http(?:Get|Post|Put|Delete|Patch)[^\]]*\]"))
            {
                var methodMatch = Regex.Match(
                    source[http.Index..],
                    @"\b(?:public|protected|internal)\s+(?:async\s+)?[\w<>,\[\]?\.\s]+\s+([A-Za-z0-9_]+)\s*\(");
                if (!methodMatch.Success)
                {
                    continue;
                }

                var methodName = methodMatch.Groups[1].Value;
                var line = source.Take(http.Index).Count(character => character == '\n') + 1;
                endpoints.Add(new HostAdminEndpoint(
                    $"{controllerName}.{methodName}",
                    $"{Path.GetRelativePath(WebApiRoot, file)}:{line}",
                    route.Groups[1].Value,
                    hasControllerPermission));
            }
        }

        return endpoints;
    }

    private static IReadOnlyList<Type> ResolveRequestTypes(MethodInfo method, string controllerSource)
    {
        var types = new List<Type>();

        foreach (var parameter in method.GetParameters())
        {
            if (IsRequestTypeName(parameter.ParameterType.Name))
            {
                types.Add(parameter.ParameterType);
            }
        }

        var body = ExtractMethodBody(controllerSource, method.Name);
        if (body is null)
        {
            return types.Distinct().ToList();
        }

        foreach (Match match in Regex.Matches(body, @"new\s+([A-Za-z_]\w*(?:Command|Query))\b"))
        {
            var type = RequestTypesByName.Value.GetValueOrDefault(match.Groups[1].Value);
            if (type is not null)
            {
                types.Add(type);
            }
        }

        return types.Distinct().ToList();
    }

    private static bool IsAnonymousRequest(Type type) =>
        typeof(IAllowAnonymousRequest).IsAssignableFrom(type);

    private static bool IsRequestTypeName(string name) =>
        name.EndsWith("Command", StringComparison.Ordinal) || name.EndsWith("Query", StringComparison.Ordinal);

    private static Dictionary<string, Type> BuildRequestTypeMap()
    {
        var map = new Dictionary<string, Type>(StringComparer.Ordinal);
        foreach (var assembly in ModuleAssemblies)
        {
            foreach (var type in assembly.GetTypes())
            {
                if (IsRequestTypeName(type.Name))
                {
                    map.TryAdd(type.Name, type);
                }
            }
        }

        return map;
    }

    private static string? ExtractMethodBody(string source, string methodName)
    {
        if (string.IsNullOrEmpty(source))
        {
            return null;
        }

        var signature = Regex.Match(source, $@"\b{Regex.Escape(methodName)}\s*\(");
        if (!signature.Success)
        {
            return null;
        }

        var start = source.IndexOf('{', signature.Index);
        if (start < 0)
        {
            return null;
        }

        var depth = 0;
        for (var index = start; index < source.Length; index++)
        {
            if (source[index] == '{') depth++;
            if (source[index] == '}')
            {
                depth--;
                if (depth == 0)
                {
                    return source[start..(index + 1)];
                }
            }
        }

        return null;
    }

    private static int FindMethodLine(string source, string methodName)
    {
        var match = Regex.Match(source, $@"\b{Regex.Escape(methodName)}\s*\(");
        return match.Success ? source.Take(match.Index).Count(character => character == '\n') + 1 : 0;
    }

    private static string? FindControllerFile(string controllersRoot, Type controller)
    {
        if (!Directory.Exists(controllersRoot))
        {
            return null;
        }

        var byName = Directory.EnumerateFiles(controllersRoot, $"{controller.Name}.cs", SearchOption.AllDirectories)
            .FirstOrDefault();
        if (byName is not null)
        {
            return byName;
        }

        // 一个文件里可能声明多个 Controller（例如 MallCouponController.cs 里还有 MallMemberCouponController）。
        var declaration = new Regex($@"\bclass\s+{Regex.Escape(controller.Name)}\b");
        return Directory.EnumerateFiles(controllersRoot, "*.cs", SearchOption.AllDirectories)
            .FirstOrDefault(path => declaration.IsMatch(File.ReadAllText(path)));
    }

    private static string CombineRoute(string? controllerRoute, string? methodRoute)
    {
        var method = methodRoute?.Trim() ?? string.Empty;
        if (method.StartsWith("api/", StringComparison.OrdinalIgnoreCase)
            || method.StartsWith("/api/", StringComparison.OrdinalIgnoreCase))
        {
            return method;
        }

        var controller = controllerRoute?.Trim() ?? string.Empty;
        return string.IsNullOrEmpty(method) ? controller : $"{controller}/{method}";
    }

    private static bool IsAdminRoute(string route) =>
        route.TrimStart('/').StartsWith("api/admin", StringComparison.OrdinalIgnoreCase);

    private static string CurrentFile([global::System.Runtime.CompilerServices.CallerFilePath] string path = "") => path;

    private sealed record HostAdminEndpoint(string Key, string Location, string Route, bool HasPermission);
}
