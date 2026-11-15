using FitCore.Domain.Enums;

namespace FitCore.Domain.Entities;

public class Discount : AuditableEntity
{
    public DiscountType Type { get; set; }
    public long Value { get; set; }
    public string? Code { get; set; }
    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }
    public string? RulesJson { get; set; }
    public int? UsageLimit { get; set; }
    public bool IsActive { get; set; }
}
