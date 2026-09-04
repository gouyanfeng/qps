using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Application.Contracts.Crm;
using QPS.Application.Contracts.Crm.CrmHerbBaseSupplies;
using QPS.Application.Features.Crm.CrmHerbBases;
using QPS.Application.Interfaces;
using QPS.Domain.Entities.Crm;
using QPS.Domain.Events.Crm;
using QPS.Domain.Exceptions;

namespace QPS.Application.Features.Crm.CrmHerbBaseSupplies;

public class GetCrmHerbBaseSuppliesQuery : IRequest<List<CrmHerbBaseSupplyDto>> { public Guid HerbBaseId { get; set; } }
public class CreateCrmHerbBaseSupplyCommand : IRequest<bool> { public Guid HerbBaseId { get; set; } public CrmHerbBaseSupplySaveRequest Request { get; set; } = null!; }
public class UpdateCrmHerbBaseSupplyCommand : IRequest<bool> { public Guid Id { get; set; } public CrmHerbBaseSupplySaveRequest Request { get; set; } = null!; }
public class DeleteCrmHerbBaseSupplyCommand : IRequest<bool> { public Guid Id { get; set; } }
public class ChangeCrmHerbBaseSupplyStatusCommand : IRequest<bool> { public Guid Id { get; set; } public CrmHerbBaseSupplyStatusRequest Request { get; set; } = null!; }

public class GetCrmHerbBaseSuppliesHandler(IDbContext dbContext) : IRequestHandler<GetCrmHerbBaseSuppliesQuery, List<CrmHerbBaseSupplyDto>>
{
    public Task<List<CrmHerbBaseSupplyDto>> Handle(GetCrmHerbBaseSuppliesQuery request, CancellationToken cancellationToken) =>
        dbContext.CrmHerbBaseSupplies.Where(item => item.HerbBaseId == request.HerbBaseId).OrderByDescending(item => item.UpdatedAt)
            .Select(item => CrmHerbBaseSupplyMapper.ToDto(item)).ToListAsync(cancellationToken);
}

public class CreateCrmHerbBaseSupplyHandler(IDbContext dbContext, IDomainEventDispatcher dispatcher) : IRequestHandler<CreateCrmHerbBaseSupplyCommand, bool>
{
    public async Task<bool> Handle(CreateCrmHerbBaseSupplyCommand command, CancellationToken cancellationToken)
    {
        var herbBase = await dbContext.CrmHerbBases.FirstOrDefaultAsync(item => item.Id == command.HerbBaseId, cancellationToken) ?? throw new BusinessException(404, "药材基地不存在");
        var request = command.Request;
        dbContext.CrmHerbBaseSupplies.Add(CrmHerbBaseSupply.Create(herbBase.Id, herbBase.HerbBaseSubjectId, request.ProductName, request.AvailableQuantity, request.QuantityUnit, request.Specification, request.QualityRequirement, request.HarvestSeason, request.ExpectedPrice, request.PriceUnit, request.SupplyCycle, request.ConfirmedAt, request.ValidUntil, request.Remark));
        await dbContext.SaveChangesAsync(cancellationToken);
        await CrmHerbBaseSupplyScoreNotifier.PublishAsync(dispatcher, herbBase.HerbBaseSubjectId, cancellationToken);
        return true;
    }
}

public class UpdateCrmHerbBaseSupplyHandler(IDbContext dbContext, IDomainEventDispatcher dispatcher) : IRequestHandler<UpdateCrmHerbBaseSupplyCommand, bool>
{
    public async Task<bool> Handle(UpdateCrmHerbBaseSupplyCommand command, CancellationToken cancellationToken)
    {
        var supply = await dbContext.CrmHerbBaseSupplies.FirstOrDefaultAsync(item => item.Id == command.Id, cancellationToken) ?? throw new BusinessException(404, "供应信息不存在");
        var request = command.Request;
        supply.Update(request.ProductName, request.AvailableQuantity, request.QuantityUnit, request.Specification, request.QualityRequirement, request.HarvestSeason, request.ExpectedPrice, request.PriceUnit, request.SupplyCycle, request.ConfirmedAt, request.ValidUntil, request.Remark);
        await dbContext.SaveChangesAsync(cancellationToken);
        await CrmHerbBaseSupplyScoreNotifier.PublishAsync(dispatcher, supply.HerbBaseSubjectId, cancellationToken);
        return true;
    }
}

public class DeleteCrmHerbBaseSupplyHandler(IDbContext dbContext, IDomainEventDispatcher dispatcher) : IRequestHandler<DeleteCrmHerbBaseSupplyCommand, bool>
{
    public async Task<bool> Handle(DeleteCrmHerbBaseSupplyCommand command, CancellationToken cancellationToken)
    {
        var supply = await dbContext.CrmHerbBaseSupplies.FirstOrDefaultAsync(item => item.Id == command.Id, cancellationToken) ?? throw new BusinessException(404, "供应信息不存在");
        if (supply.Status != CrmHerbBaseSupply.Pending) throw new BusinessException(400, "只有待确认供应信息可以删除");
        supply.IsDeleted = true;
        await dbContext.SaveChangesAsync(cancellationToken);
        await CrmHerbBaseSupplyScoreNotifier.PublishAsync(dispatcher, supply.HerbBaseSubjectId, cancellationToken);
        return true;
    }
}

public class ChangeCrmHerbBaseSupplyStatusHandler(IDbContext dbContext, IDomainEventDispatcher dispatcher) : IRequestHandler<ChangeCrmHerbBaseSupplyStatusCommand, bool>
{
    public async Task<bool> Handle(ChangeCrmHerbBaseSupplyStatusCommand command, CancellationToken cancellationToken)
    {
        var supply = await dbContext.CrmHerbBaseSupplies.FirstOrDefaultAsync(item => item.Id == command.Id, cancellationToken) ?? throw new BusinessException(404, "供应信息不存在");
        supply.ChangeStatus(command.Request.Status);
        await dbContext.SaveChangesAsync(cancellationToken);
        await CrmHerbBaseSupplyScoreNotifier.PublishAsync(dispatcher, supply.HerbBaseSubjectId, cancellationToken);
        return true;
    }
}

internal static class CrmHerbBaseSupplyScoreNotifier
{
    public static async Task PublishAsync(
        IDomainEventDispatcher dispatcher,
        Guid? subjectId,
        CancellationToken cancellationToken)
    {
        if (subjectId.HasValue)
        {
            await dispatcher.PublishAsync(new CrmHerbBaseSubjectScoreAffectedEvent(subjectId.Value), cancellationToken);
        }
    }
}

public static class CrmHerbBaseSupplyMapper
{
    public static CrmHerbBaseSupplyDto ToDto(CrmHerbBaseSupply item) => new() { Id = item.Id, HerbBaseId = item.HerbBaseId, HerbBaseSubjectId = item.HerbBaseSubjectId, ProductName = item.ProductName, AvailableQuantity = item.AvailableQuantity, QuantityUnit = item.QuantityUnit, Specification = item.Specification, QualityRequirement = item.QualityRequirement, HarvestSeason = item.HarvestSeason, ExpectedPrice = item.ExpectedPrice, PriceUnit = item.PriceUnit, SupplyCycle = item.SupplyCycle, ConfirmedAt = item.ConfirmedAt, ValidUntil = item.ValidUntil, Status = item.Status, Remark = item.Remark, IsExpired = item.Status == CrmHerbBaseSupply.Active && item.ValidUntil < DateTime.Today };
}
