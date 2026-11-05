using QPS.Crm.Contracts;
using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Crm.Contracts.DTOs;
using QPS.Crm.Contracts.DTOs.CrmHerbBases;
using QPS.Crm.Application.Features;
using QPS.Shared.Contracts;
using QPS.Shared.Exceptions;

namespace QPS.Crm.Application.Features.CrmHerbBases;

/// <summary>
/// 获取药材基地详情查询
/// </summary>
public class GetCrmHerbBaseQuery : IRequest<CrmHerbBaseDto>
{
    /// <summary>
    /// 客户ID
    /// </summary>
    public Guid Id { get; set; }
}

/// <summary>
/// 获取药材基地详情处理器
/// </summary>
public class GetCrmHerbBaseHandler : IRequestHandler<GetCrmHerbBaseQuery, CrmHerbBaseDto>
{
    private readonly ICrmDbContext _dbContext;

    /// <summary>
    /// 获取药材基地详情处理器。
    /// </summary>
    public GetCrmHerbBaseHandler(ICrmDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// 编排查询药材基地详情用例。
    /// </summary>
    public async Task<CrmHerbBaseDto> Handle(GetCrmHerbBaseQuery request, CancellationToken cancellationToken)
    {
        // 编排查询药材基地详情用例：
        // 查询基础资料并校验基地存在。
        var dto = await GetCustomerDto(request.Id, cancellationToken);
        await FillOwnerNameAsync(dto, cancellationToken);
        return dto;
    }

    private async Task FillOwnerNameAsync(CrmHerbBaseDto dto, CancellationToken cancellationToken)
    {
        if (!dto.OwnerUserId.HasValue)
        {
            return;
        }

        var users = await CrmUserNames.GetAsync(_dbContext, [dto.OwnerUserId.Value], cancellationToken);
        if (users.TryGetValue(dto.OwnerUserId.Value, out var owner))
        {
            dto.OwnerUserName = owner.RealName != string.Empty ? owner.RealName : owner.Username;
        }
    }

    /// <summary>
    /// 查询药材基地详情 DTO。
    /// </summary>
    private async Task<CrmHerbBaseDto> GetCustomerDto(Guid customerId, CancellationToken cancellationToken)
    {
        var dto = await (
            from customer in _dbContext.CrmHerbBases
            where customer.Id == customerId
            select new CrmHerbBaseDto
            {
                Id = customer.Id,
                BaseName = customer.BaseName,
                SubjectName = customer.SubjectName,
                Grade = customer.Grade,
                Score = customer.Score,
                Scale = customer.Scale,
                Province = customer.Province,
                City = customer.City,
                Area = customer.Area,
                Address = customer.Address,
                Lat = customer.Lat,
                Lng = customer.Lng,
                SourcePlatform = customer.SourcePlatform,
                SourceId = customer.SourceId,
                Status = customer.Status,
                OwnerUserId = customer.OwnerUserId,
                Remark = customer.Remark,
                PrimaryContactName = customer.PrimaryContactName,
                PrimaryContactPhone = customer.PrimaryContactPhone,
                LastFollowAt = customer.LastFollowAt,
                LastFollowResult = customer.LastFollowResult,
                NextFollowAt = customer.NextFollowAt,
                CreatedAt = customer.CreatedAt,
                UpdatedAt = customer.UpdatedAt
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (dto == null)
        {
            throw new BusinessException(404, "药材基地不存在");
        }

        return dto;
    }

}
