using Microsoft.EntityFrameworkCore;
using QPS.Mall.Contracts;
using QPS.Mall.Contracts.DTOs;
using QPS.Mall.Domain;
using QPS.Mall.Domain.Entities;
using QPS.Shared.Exceptions;

namespace QPS.Mall.Application.Features.MallReturns;

/// <summary>退货单统一装配：按 ID/单号解析与 DTO 映射的唯一实现。</summary>
internal static class MallReturnMapper
{
    public static async Task<MallReturn> ResolveAsync(
        IMallDbContext dbContext,
        Guid? returnId,
        string? returnNo,
        CancellationToken cancellationToken)
    {
        MallReturn? returnEntity;

        if (returnId.HasValue)
        {
            returnEntity = await dbContext.MallReturns
                .FirstOrDefaultAsync(x => x.Id == returnId.Value, cancellationToken);
        }
        else if (!string.IsNullOrWhiteSpace(returnNo))
        {
            var normalized = returnNo.Trim();
            returnEntity = await dbContext.MallReturns
                .FirstOrDefaultAsync(x => x.ReturnNo == normalized, cancellationToken);
        }
        else
        {
            throw new BusinessException(400, "退货单标识不能为空");
        }

        if (returnEntity is null)
        {
            throw new BusinessException(404, "退货单不存在");
        }

        return returnEntity;
    }

    public static MallReturnDto ToDto(MallReturn returnEntity)
    {
        return new MallReturnDto
        {
            Id = returnEntity.Id,
            ReturnNo = returnEntity.ReturnNo,
            OrderId = returnEntity.OrderId,
            PaymentId = returnEntity.PaymentId,
            RefundId = returnEntity.RefundId,
            Amount = returnEntity.Amount,
            Reason = returnEntity.Reason,
            Description = returnEntity.Description,
            EvidenceImages = MallAfterSaleRules.SplitEvidence(returnEntity.EvidenceImages),
            RejectReason = returnEntity.RejectReason,
            Status = returnEntity.Status,
            ExpressCompany = returnEntity.ExpressCompany,
            TrackingNo = returnEntity.TrackingNo,
            ApprovedAt = returnEntity.ApprovedAt,
            RejectedAt = returnEntity.RejectedAt,
            ShippedAt = returnEntity.ShippedAt,
            ReceivedAt = returnEntity.ReceivedAt,
            CompletedAt = returnEntity.CompletedAt,
            CreatedAt = returnEntity.CreatedAt
        };
    }
}
