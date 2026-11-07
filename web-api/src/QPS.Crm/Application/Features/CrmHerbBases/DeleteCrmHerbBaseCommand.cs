using QPS.Crm.Contracts;
using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Shared.Contracts;
using QPS.Crm.Domain.Entities;
using QPS.Crm.Domain.Events;
using QPS.Shared.Exceptions;

namespace QPS.Crm.Application.Features.CrmHerbBases;

public class DeleteCrmHerbBaseCommand : IRequest<bool>
{
    public Guid Id { get; set; }
}

public class DeleteCrmHerbBaseHandler : IRequestHandler<DeleteCrmHerbBaseCommand, bool>
{
    private readonly ICrmDbContext _dbContext;
    private readonly IDomainEventDispatcher _dispatcher;

    public DeleteCrmHerbBaseHandler(ICrmDbContext dbContext, IDomainEventDispatcher dispatcher)
    {
        _dbContext = dbContext;
        _dispatcher = dispatcher;
    }

    public async Task<bool> Handle(DeleteCrmHerbBaseCommand request, CancellationToken cancellationToken)
    {
        var customer = await GetCustomer(request.Id, cancellationToken);

        customer.SoftDelete();
        await SyncSubjectScaleAsync(customer, cancellationToken);
        
        await _dbContext.SaveChangesAsync(cancellationToken);
        if (customer.HerbBaseSubjectId.HasValue)
        {
            await _dispatcher.PublishAsync(new CrmHerbBaseSubjectScoreAffectedEvent(customer.HerbBaseSubjectId.Value), cancellationToken);
        }

        return true;
    }

    private async Task<CrmHerbBase> GetCustomer(Guid customerId, CancellationToken cancellationToken)
    {
        var customer = await _dbContext.CrmHerbBases
            .FirstOrDefaultAsync(c => c.Id == customerId, cancellationToken);

        if (customer == null)
        {
            throw new BusinessException(404, "药材基地不存在");
        }

        return customer;
    }

    private async Task SyncSubjectScaleAsync(CrmHerbBase herbBase, CancellationToken cancellationToken)
    {
        if (!herbBase.HerbBaseSubjectId.HasValue)
        {
            return;
        }

        var subject = await _dbContext.CrmHerbBaseSubjects
            .FirstOrDefaultAsync(item => item.Id == herbBase.HerbBaseSubjectId.Value, cancellationToken);
        if (subject == null)
        {
            return;
        }

        var remainingScale = await _dbContext.CrmHerbBases
            .Where(item =>
                item.HerbBaseSubjectId == herbBase.HerbBaseSubjectId.Value &&
                item.Id != herbBase.Id)
            .SumAsync(item => item.Scale ?? 0, cancellationToken);

        subject.UpdateScale(remainingScale);
    }
}
