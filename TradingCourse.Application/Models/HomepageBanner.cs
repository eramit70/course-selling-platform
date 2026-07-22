using System;
using System.ComponentModel.DataAnnotations;

namespace TradingCourse.Application.Models;

public class HomepageBanner
{
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Heading { get; set; } = string.Empty;

    [Required]
    [MaxLength(250)]
    public string Subheading { get; set; } = string.Empty;

    [RegularExpression("^#[0-9A-Fa-f]{6}$", ErrorMessage = "Heading color must be a valid hexadecimal color.")]
    [MaxLength(7)]
    public string? HeadingColor { get; set; } = "#FFFFFF";

    [RegularExpression("^#[0-9A-Fa-f]{6}$", ErrorMessage = "Subheading color must be a valid hexadecimal color.")]
    [MaxLength(7)]
    public string? SubheadingColor { get; set; } = "#CBD5E1";

    [Required]
    [MaxLength(20)]
    public string MediaType { get; set; } = "Photo"; // "Photo" or "Video"

    public string MediaUrl { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? ButtonText { get; set; }

    [MaxLength(500)]
    public string? ButtonUrl { get; set; }

    [RegularExpression("^#[0-9A-Fa-f]{6}$", ErrorMessage = "Button color must be a valid hexadecimal color.")]
    [MaxLength(7)]
    public string? ButtonColor { get; set; } = "#14B8A6";

    public int DisplayOrder { get; set; } = 0;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
