using System.Text.RegularExpressions;
using Xunit;

namespace QPS.ArchitectureTests;

/// <summary>
/// 模块“注册即启用”：Host 启动装配必须调用 AddQpsSystem / AddQpsCrm / AddQpsMall，
/// 漏掉任一调用时对应模块的 Handler 与 Controller 都不会参与运行。
/// </summary>
public class ModuleWiringTests
{
    private static readonly string[] RequiredRegistrations =
    {
        "AddQpsSystem(",
        "AddQpsCrm(",
        "AddQpsMall("
    };

    private static string WebApiRoot =>
        Path.GetFullPath(Path.Combine(Path.GetDirectoryName(CurrentFile())!, "..", ".."));

    [Fact]
    public void HostStartup_ShouldRegisterAllModules()
    {
        var programPath = Path.Combine(WebApiRoot, "src", "QPS.Host", "Program.cs");
        Assert.True(File.Exists(programPath), $"未找到启动装配文件：{programPath}");

        var source = StripComments(File.ReadAllText(programPath));
        var missing = RequiredRegistrations
            .Where(registration => !source.Contains(registration, StringComparison.Ordinal))
            .ToList();

        Assert.True(
            missing.Count == 0,
            $"启动装配缺少模块注册：{string.Join(", ", missing)}（应在 src/QPS.Host/Program.cs 中调用）");
    }

    private static string StripComments(string source)
    {
        var withoutBlockComments = Regex.Replace(source, @"/\*.*?\*/", string.Empty, RegexOptions.Singleline);
        return Regex.Replace(withoutBlockComments, @"//[^\n]*", string.Empty);
    }

    private static string CurrentFile([global::System.Runtime.CompilerServices.CallerFilePath] string path = "") => path;
}
