using System.Text.RegularExpressions;
using Xunit;

namespace QPS.ArchitectureTests;

/// <summary>
/// Application 层 Handler 禁止手写事务：事务边界统一由 Shared 的 TransactionBehaviour 管理，
/// Handler 自行 Begin/Commit/Rollback 会绕过统一提交/回滚策略。
/// </summary>
public class HandlerTransactionTests
{
    private static readonly string[] TransactionTokens =
    {
        "BeginTransaction",
        "CommitTransaction",
        "RollbackTransaction"
    };

    private static string WebApiRoot =>
        Path.GetFullPath(Path.Combine(Path.GetDirectoryName(CurrentFile())!, "..", ".."));

    [Fact]
    public void ApplicationHandlers_ShouldNotManageTransactionsManually()
    {
        var violations = new List<string>();

        foreach (var module in new[] { "QPS.System", "QPS.Crm", "QPS.Mall" })
        {
            var applicationRoot = Path.Combine(WebApiRoot, "src", module, "Application");
            if (!Directory.Exists(applicationRoot))
            {
                continue;
            }

            foreach (var file in Directory.EnumerateFiles(applicationRoot, "*.cs", SearchOption.AllDirectories))
            {
                if (file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                    || file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
                {
                    continue;
                }

                var source = StripComments(File.ReadAllText(file));
                foreach (var token in TransactionTokens)
                {
                    var index = source.IndexOf(token, StringComparison.Ordinal);
                    if (index < 0)
                    {
                        continue;
                    }

                    var line = source.Take(index).Count(character => character == '\n') + 1;
                    violations.Add(
                        $"{Path.GetRelativePath(WebApiRoot, file)}:{line} Application 层出现手写事务 {token}");
                }
            }
        }

        Assert.Empty(violations);
    }

    private static string StripComments(string source)
    {
        var withoutBlockComments = Regex.Replace(source, @"/\*.*?\*/", string.Empty, RegexOptions.Singleline);
        return Regex.Replace(withoutBlockComments, "//[^\n]*", string.Empty);
    }

    private static string CurrentFile([global::System.Runtime.CompilerServices.CallerFilePath] string path = "") => path;
}
