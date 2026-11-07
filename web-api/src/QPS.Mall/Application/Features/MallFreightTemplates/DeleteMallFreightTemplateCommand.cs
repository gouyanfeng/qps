using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Mall.Contracts;
using QPS.Mall.Domain.Entities;
using QPS.Shared.Exceptions;

namespace QPS.Mall.Application.Features.MallFreightTemplates;

public class DeleteMallFreightTemplateCommand : IRequest<bool>
{
    public Guid TemplateId { get; set; }
}

public class DeleteMallFreightTemplateHandler : IRequestHandler<DeleteMallFreightTemplateCommand, bool>
{
    private readonly IMallDbContext _dbContext;

    public DeleteMallFreightTemplateHandler(IMallDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> Handle(DeleteMallFreightTemplateCommand command, CancellationToken cancellationToken)
    {
        var template = await _dbContext.MallFreightTemplates
            .FirstOrDefaultAsync(t => t.Id == command.TemplateId, cancellationToken);

        if (template is null)
        {
            throw new BusinessException(404, "运费模板不存在");
        }

        var productCount = await _dbContext.MallProducts.CountAsync(
            p => p.FreightTemplateId == template.Id,
            cancellationToken);

        if (productCount > 0)
        {
            throw new BusinessException(400, $"有 {productCount} 个商品使用此模板，无法删除");
        }

        template.Update(template.Name, template.BillingType, false, false);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }
}