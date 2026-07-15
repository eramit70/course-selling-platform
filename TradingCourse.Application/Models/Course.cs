using System;
using System.ComponentModel.DataAnnotations;

namespace TradingCourse.Application.Models;

public class Course
{
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Slug { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string ShortDescription { get; set; } = string.Empty;

    [Required]
    public string FullDescription { get; set; } = string.Empty;

    public string? ThumbnailUrl { get; set; }

    [Range(0, 1000000)]
    public decimal Price { get; set; }

    [Range(0, 1000000)]
    public decimal? SalePrice { get; set; }

    public bool IsLive { get; set; }

    [Required]
    [MaxLength(100)]
    public string Instructor { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Schedule { get; set; } = string.Empty;

    public string? LiveClassLink { get; set; }

    [MaxLength(50)]
    public string? MediaType { get; set; }

    public string? MediaUrl { get; set; }

    [MaxLength(200)]
    public string? EmailTemplateSubject { get; set; }

    public string? EmailTemplateBody { get; set; }

    public int PurchaseDurationDays { get; set; } = 365;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
