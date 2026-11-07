using System.Reflection;
using System.Text.RegularExpressions;
using Xunit;

namespace QPS.ArchitectureTests;

/// <summary>
/// 学员端（MallMember）与后台（AdminOnly）之间的安全边界回归约束：
/// 后台控制器必须显式声明 AdminOnly 策略；移动端订单/退货类命令必须携带 MemberId 以便做归属校验。
/// </summary>
public class MallSecurityTests
{
    private static string WebApiRoot =>
        Path.GetFullPath(Path.Combine(Path.GetDirectoryName(CurrentFile())!, "..", ".."));

    private static Assembly MallAssembly => typeof(QPS.Mall.Controllers.Mobile.MallReturnExternalController).Assembly;

    [Fact]
    public void AdminControllers_ShouldDeclareAdminOnlyPolicy()
    {
        var violations = new List<string>();

        var controllers = MallAssembly.GetTypes()
            .Where(type => type.IsClass && type.IsPublic && type.Name.EndsWith("Controller", StringComparison.Ordinal))
            .Where(IsAdminController)
            .ToList();

        Assert.NotEmpty(controllers);

        foreach (var controller in controllers)
        {
            var authorized = controller.GetCustomAttributes(true)
                .Where(attribute => attribute.GetType().Name == "AuthorizeAttribute")
                .Any(attribute => ReadStringProperty(attribute, "Policy") == "AdminOnly");

            if (!authorized)
            {
                violations.Add($"{controller.FullName} 未声明 [Authorize(Policy = \"AdminOnly\")]");
            }
        }

        Assert.Empty(violations);
    }

    [Fact]
    public void MobileOrderAndReturnCommands_ShouldCarryMemberId()
    {
        var mobileRoot = Path.Combine(WebApiRoot, "src", "QPS.Mall", "Controllers", "Mobile");
        Assert.True(Directory.Exists(mobileRoot), $"未找到移动端控制器目录：{mobileRoot}");

        var commandNames = new HashSet<string>(StringComparer.Ordinal);
        var initializers = new List<(string File, string Command, string Body)>();

        foreach (var file in Directory.EnumerateFiles(mobileRoot, "*.cs", SearchOption.AllDirectories))
        {
            var content = File.ReadAllText(file);

            foreach (Match match in Regex.Matches(content, @"new\s+(\w+Command)\s*\{(?<body>[^}]*)\}", RegexOptions.Singleline))
            {
                commandNames.Add(match.Groups[1].Value);
                initializers.Add((file, match.Groups[1].Value, match.Groups["body"].Value));
            }
        }

        Assert.NotEmpty(commandNames);
        Assert.NotEmpty(initializers);

        var protectedCommands = MallAssembly.GetTypes()
            .Where(type => type.IsClass && type.IsPublic
                && commandNames.Contains(type.Name)
                && HasIdentifierProperty(type))
            .ToList();

        Assert.NotEmpty(protectedCommands);

        var violations = protectedCommands
            .Where(type => NoProperty(type, "MemberId"))
            .Select(type => $"{type.Name} 缺少 MemberId 属性")
            .ToList();

        foreach (var (file, command, body) in initializers)
        {
            if (!protectedCommands.Any(type => type.Name == command)) continue;
            if (Regex.IsMatch(body, @"MemberId\s*=")) continue;

            violations.Add($"{Path.GetRelativePath(WebApiRoot, file)} 中 new {command} 未传入 MemberId");
        }

        Assert.Empty(violations);
    }

    private static bool HasIdentifierProperty(Type type) =>
        new[] { "OrderId", "OrderNo", "ReturnNo", "RefundNo", "PaymentNo" }
            .Any(name => type.GetProperty(name) is not null);

    private static bool NoProperty(Type type, string name) => type.GetProperty(name) is null;

    private static bool IsAdminController(Type controller) =>
        controller.GetCustomAttributes(true).Any(IsAdminRoute)
        || controller.GetMethods()
            .SelectMany(method => method.GetCustomAttributes(true))
            .Any(IsAdminRoute);

    private static bool IsAdminRoute(object attribute)
    {
        var typeName = attribute.GetType().Name;
        if (!typeName.StartsWith("Http", StringComparison.Ordinal) && typeName != "RouteAttribute") return false;

        var template = ReadStringProperty(attribute, "Template")
            ?? ReadStringProperty(attribute, "RouteTemplate");

        return template is not null && template.StartsWith("api/admin", StringComparison.OrdinalIgnoreCase);
    }

    private static string? ReadStringProperty(object instance, string propertyName)
    {
        var property = instance.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
        return property?.GetValue(instance) as string;
    }

    private static string CurrentFile([global::System.Runtime.CompilerServices.CallerFilePath] string path = "") => path;
}
