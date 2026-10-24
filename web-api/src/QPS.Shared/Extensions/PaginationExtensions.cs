using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using QPS.Shared.Common;

namespace QPS.Shared.Extensions;

/// <summary>
/// 分页扩展方法
/// </summary>
public static class PaginationExtensions
{
    /// <summary>
    /// 应用分页和排序
    /// </summary>
    /// <typeparam name="T">实体类型</typeparam>
    /// <param name="query">查询对象</param>
    /// <param name="request">分页请求</param>
    /// <returns>分页后的查询对象</returns>
    public static IQueryable<T> ApplyPaginationAndSorting<T>(this IQueryable<T> query, PaginationRequest request)
    {
        // 排序
        query = ApplySorting(query, request.SortField, request.SortDirection);

        // 分页
        var skip = (request.Page - 1) * request.PageSize;
        query = query.Skip(skip).Take(request.PageSize);

        return query;
    }

    /// <summary>
    /// 应用排序
    /// </summary>
    /// <typeparam name="T">实体类型</typeparam>
    /// <param name="query">查询对象</param>
    /// <param name="sortField">排序字段</param>
    /// <param name="sortDirection">排序方向</param>
    /// <returns>排序后的查询对象</returns>
    private static IQueryable<T> ApplySorting<T>(IQueryable<T> query, string sortField, string sortDirection)
    {
        var isAscending = sortDirection.Equals("Ascending", StringComparison.OrdinalIgnoreCase);

        try
        {
            // 构建排序表达式
            var parameter = System.Linq.Expressions.Expression.Parameter(typeof(T), "e");
            var property = System.Linq.Expressions.Expression.Property(parameter, sortField);
            var lambda = System.Linq.Expressions.Expression.Lambda<Func<T, object>>(System.Linq.Expressions.Expression.Convert(property, typeof(object)), parameter);

            if (isAscending)
            {
                query = query.OrderBy(lambda);
            }
            else
            {
                query = query.OrderByDescending(lambda);
            }
        }
        catch
        {
            // 如果排序字段不存在，使用默认排序
            try
            {
                var parameter = System.Linq.Expressions.Expression.Parameter(typeof(T), "e");
                var defaultProperty = System.Linq.Expressions.Expression.Property(parameter, "CreatedAt");
                var defaultLambda = System.Linq.Expressions.Expression.Lambda<Func<T, object>>(System.Linq.Expressions.Expression.Convert(defaultProperty, typeof(object)), parameter);
                query = query.OrderByDescending(defaultLambda);
            }
            catch
            {
                // 如果默认排序字段也不存在，不进行排序
            }
        }

        return query;
    }

    /// <summary>
    /// 执行分页查询
    /// </summary>
    /// <typeparam name="T">实体类型</typeparam>
    /// <param name="query">查询对象</param>
    /// <param name="request">分页请求</param>
    /// <returns>分页响应</returns>
    public static async Task<PaginationResponse<T>> ToPaginationResponseAsync<T>(
        this IQueryable<T> query,
        PaginationRequest request,
        CancellationToken cancellationToken = default)
    {
        // 先取总数，再对同一查询应用分页与排序，避免重复构造查询。
        var totalCount = await query.CountAsync(cancellationToken);

        var paginatedQuery = query.ApplyPaginationAndSorting(request);

        var data = await paginatedQuery.ToListAsync(cancellationToken);

        return new PaginationResponse<T>(data, totalCount, request.Page, request.PageSize);
    }
}

