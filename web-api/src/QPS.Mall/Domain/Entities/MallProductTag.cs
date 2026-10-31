using QPS.Shared.Common;

namespace QPS.Mall.Domain.Entities;

public class MallProductTag : BaseEntity
{
    public Guid ProductId { get; private set; }

    public Guid TagId { get; private set; }

    private MallProductTag()
    {
    }

    private MallProductTag(Guid productId, Guid tagId)
    {
        ProductId = productId;
        TagId = tagId;
    }

    public static MallProductTag Create(Guid productId, Guid tagId)
        => new(productId, tagId);
}