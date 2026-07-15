using System;
using System.ComponentModel.DataAnnotations;

namespace TradingCourse.Application.Models;

public enum PaymentStatus
{
    Pending,
    Success,
    Failed
}

public class Order
{
    public int Id { get; set; }

    public int CourseId { get; set; }
    public Course Course { get; set; } = null!;

    [Required]
    [MaxLength(100)]
    public string CustomerName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [MaxLength(150)]
    public string CustomerEmail { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string CustomerMobile { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? CouponCode { get; set; }

    [Range(0, 1000000)]
    public decimal AmountPaid { get; set; }

    [MaxLength(100)]
    public string? RazorpayOrderId { get; set; }

    [MaxLength(100)]
    public string? RazorpayPaymentId { get; set; }

    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
