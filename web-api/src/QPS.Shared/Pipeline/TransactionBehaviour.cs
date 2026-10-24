using MediatR;
using QPS.Shared.Contracts;

namespace QPS.Shared.Pipeline;

/// <summary>
/// 事务行为：命令执行前后包裹数据库事务。
/// 现有 CQRS 命名约定为 XxxCommand / XxxQuery，查询请求不开启写事务。
/// </summary>
public class TransactionBehaviour<TRequest, TResponse>(
    IUnitOfWork unitOfWork) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (request.GetType().Name.EndsWith("Query", StringComparison.Ordinal))
        {
            return await next(cancellationToken);
        }

        await unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var response = await next(cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);
            return response;
        }
        catch
        {
            await unitOfWork.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
