namespace QPS.Crm.Contracts.DTOs.CrmHerbBases;

public class CrmHerbBaseMapDto
{
    public Guid Id { get; set; }
    public string BaseName { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public decimal Lat { get; set; }
    public decimal Lng { get; set; }
}