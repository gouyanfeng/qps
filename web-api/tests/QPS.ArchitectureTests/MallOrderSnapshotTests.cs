using System.Text.RegularExpressions;
using Xunit;

namespace QPS.ArchitectureTests;

/// <summary>
/// 订单列表 / 详情查询只能读取下单时写入订单表的快照字段，
/// 不得回查商品主表或促销/活动表；否则历史订单展示会随后续改价、下架、活动结束而漂移。
/// </summary>
public class MallOrderSnapshotTests
{
    private static readonly string[] ForbiddenTables =
    {
        "MallProducts",
        "MallPromotions",
        "MallPromotionScopes",
        "MallCampaigns",
        "MallCampaignProducts",
        "MallCampaignCoupons",
        "MallCoupons",
        "MallMemberCoupons"
    };

    private static string WebApiRoot =>
        Path.GetFullPath(Path.Combine(Path.GetDirectoryName(CurrentFile())!, "..", ".."));

    [Fact]
    public void OrderListAndDetailQueries_ShouldReadSnapshotsOnly()
    {
        var ordersRoot = Path.Combine(WebApiRoot, "src", "QPS.Mall", "Application", "Features", "MallOrders");
        Assert.True(Directory.Exists(ordersRoot), $"未找到订单特性目录：{ordersRoot}");

        var files = Directory.EnumerateFiles(ordersRoot, "*.cs")
            .Where(path => Path.GetFileName(path).StartsWith("GetMallOrder", StringComparison.Ordinal)
                || Path.GetFileName(path) == "MallOrderDetailLoader.cs")
            .ToList();

        Assert.NotEmpty(files);
        Assert.Contains(files, path => Path.GetFileName(path) == "GetMallOrdersQuery.cs");
        Assert.Contains(files, path => Path.GetFileName(path) == "GetMallOrderQuery.cs");
        Assert.Contains(files, path => Path.GetFileName(path) == "MallOrderDetailLoader.cs");

        var violations = new List<string>();
        foreach (var file in files)
        {
            var source = StripComments(File.ReadAllText(file));
            foreach (var table in ForbiddenTables)
            {
                var match = Regex.Match(source, $@"\b{Regex.Escape(table)}\b");
                if (!match.Success)
                {
                    continue;
                }

                var line = source.Take(match.Index).Count(character => character == '\n') + 1;
                violations.Add(
                    $"{Path.GetRelativePath(WebApiRoot, file)}:{line} 订单读取路径引用了 {table}，应只读订单快照字段");
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
