using System;
using System.ComponentModel.DataAnnotations;

namespace TradingCourse.Application.Models;

public enum DiscountType
{
    Percentage,
    Fixed
}

public class Coupon
{
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string Code { get; set; } = string.Empty;

    public DiscountType DiscountType { get; set; }

    [Range(0, 1000000)]
    public decimal DiscountValue { get; set; }

    public DateTime? ExpiryDate { get; set; }

    public int? MaxUsage { get; set; }

    public int UsedCount { get; set; }

    [Range(0, 1000000)]
    public decimal? MinPurchaseAmount { get; set; }

    public int? CourseId { get; set; }
    public Course? Course { get; set; }
}
