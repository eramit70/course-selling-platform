using System;

namespace TradingCourse.Shared;

public class CouponValidateRequest
{
    public string Code { get; set; } = string.Empty;
    public int CourseId { get; set; }
    public decimal OrderAmount { get; set; }
}

public class CouponValidateResponse
{
    public decimal DiscountAmount { get; set; }
    public decimal DiscountedAmount { get; set; }
}

public class OrderCreateRequest
{
    public int CourseId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public string CustomerMobile { get; set; } = string.Empty;
    public string? CouponCode { get; set; }
}

public class OrderCreateResponse
{
    public string RazorpayKey { get; set; } = string.Empty;
    public string RazorpayOrderId { get; set; } = string.Empty;
    public int Amount { get; set; } // in paisa
    public string CourseTitle { get; set; } = string.Empty;
    public bool IsSimulator { get; set; }
}

public class PaymentVerifyRequest
{
    public string RazorpayOrderId { get; set; } = string.Empty;
    public string RazorpayPaymentId { get; set; } = string.Empty;
    public string RazorpaySignature { get; set; } = string.Empty;
}

public class PaymentVerifyResponse
{
    public int OrderId { get; set; }
}

public class ResendDetailsRequest
{
    public string Email { get; set; } = string.Empty;
    public int CourseId { get; set; }
}
