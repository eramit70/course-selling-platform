using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Hangfire;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Razorpay.Api;
using TradingCourse.Application;
using TradingCourse.Application.Models;
using TradingCourse.Application.Services;
using TradingCourse.Shared;

namespace TradingCourse.Web.Controllers;

[ApiController]
[Route("api/checkout")]
public class CheckoutApiController : BaseApiController
{
    private readonly AppDbContext _context;
    private readonly ICouponService _couponService;
    private readonly IConfiguration _configuration;

    public CheckoutApiController(
        AppDbContext context,
        ICouponService couponService,
        IConfiguration configuration)
    {
        _context = context;
        _couponService = couponService;
        _configuration = configuration;
    }

    [HttpPost("validate-coupon")]
    public async Task<IActionResult> ValidateCoupon([FromBody] CouponValidateRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Code))
        {
            return ErrorResponse("Coupon code cannot be empty.", 400);
        }

        var validation = await _couponService.ValidateCouponAsync(request.Code, request.CourseId, request.OrderAmount);
        if (!validation.IsValid)
        {
            return ErrorResponse(validation.Message, 400);
        }

        var discount = await _couponService.CalculateDiscountAsync(request.Code, request.CourseId, request.OrderAmount);
        var discountedAmount = request.OrderAmount - discount;
        if (discountedAmount < 0) discountedAmount = 0;

        var response = new CouponValidateResponse
        {
            DiscountAmount = discount,
            DiscountedAmount = discountedAmount
        };

        return SuccessResponse(response, validation.Message);
    }

    [HttpPost("create-order")]
    public async Task<IActionResult> CreateOrder([FromBody] OrderCreateRequest request)
    {
        if (request == null)
        {
            return ErrorResponse("Invalid order request.", 400);
        }

        // 1. Validation checks
        if (string.IsNullOrWhiteSpace(request.CustomerName))
        {
            return ErrorResponse("Full Name is required.", 400);
        }

        if (string.IsNullOrWhiteSpace(request.CustomerEmail) || !new EmailAddressAttribute().IsValid(request.CustomerEmail))
        {
            return ErrorResponse("Please enter a valid email address.", 400);
        }

        // Indian mobile validation (10 digits starting with 6-9)
        if (string.IsNullOrWhiteSpace(request.CustomerMobile) || !Regex.IsMatch(request.CustomerMobile.Trim(), @"^[6-9]\d{9}$"))
        {
            return ErrorResponse("Please enter a valid 10-digit Indian mobile number.", 400);
        }

        // 2. Fetch Course
        var course = await _context.Courses.FindAsync(request.CourseId);
        if (course == null || !course.IsLive)
        {
            return ErrorResponse("Course is currently not available for enrollment.", 404);
        }

        // 3. Duplicate active registration check
        var emailNormalized = request.CustomerEmail.Trim().ToUpperInvariant();
        var activeEnrolment = await _context.Orders
            .Where(o => o.CourseId == request.CourseId && 
                        o.CustomerEmail.ToUpper() == emailNormalized && 
                        o.PaymentStatus == PaymentStatus.Success)
            .OrderByDescending(o => o.CreatedAt)
            .FirstOrDefaultAsync();

        if (activeEnrolment != null)
        {
            var expirationDate = activeEnrolment.CreatedAt.AddDays(course.PurchaseDurationDays);
            if (expirationDate > DateTime.UtcNow)
            {
                return StatusCode(409, new ApiResponse<object>
                {
                    Success = false,
                    StatusCode = 409,
                    Message = "You have already enrolled in this course."
                });
            }
        }

        // 4. Calculate pricing
        decimal baseAmount = course.SalePrice ?? course.Price;
        decimal discountAmount = 0;

        if (!string.IsNullOrWhiteSpace(request.CouponCode))
        {
            var couponValidation = await _couponService.ValidateCouponAsync(request.CouponCode, request.CourseId, baseAmount);
            if (couponValidation.IsValid)
            {
                discountAmount = await _couponService.CalculateDiscountAsync(request.CouponCode, request.CourseId, baseAmount);
            }
        }

        decimal finalAmount = baseAmount - discountAmount;
        if (finalAmount < 0) finalAmount = 0;

        // 5. Create local pending order record
        var localOrder = new TradingCourse.Application.Models.Order
            {
                CourseId = course.Id,
                CustomerName = request.CustomerName.Trim(),
                CustomerEmail = request.CustomerEmail.Trim().ToLowerInvariant(),
                CustomerMobile = request.CustomerMobile.Trim(),
                CouponCode = !string.IsNullOrWhiteSpace(request.CouponCode) ? request.CouponCode.Trim().ToUpperInvariant() : null,
                AmountPaid = finalAmount,
                PaymentStatus = PaymentStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

        _context.Orders.Add(localOrder);
        await _context.SaveChangesAsync();

        // 6. Razorpay order mapping / Simulator check
        var keyId = _configuration["Razorpay:KeyId"];
        var keySecret = _configuration["Razorpay:KeySecret"];

        var isSimulator = string.IsNullOrEmpty(keyId) || 
                          string.IsNullOrEmpty(keySecret) || 
                          keyId.StartsWith("YOUR_", StringComparison.OrdinalIgnoreCase);

        var orderResponse = new OrderCreateResponse
        {
            CourseTitle = course.Title,
            Amount = Convert.ToInt32(finalAmount * 100) // amount in paisa
        };

        if (isSimulator)
        {
            var mockOrderId = "order_mock_" + Guid.NewGuid().ToString().Replace("-", "").Substring(0, 14);
            localOrder.RazorpayOrderId = mockOrderId;
            await _context.SaveChangesAsync();

            orderResponse.RazorpayKey = "simulated_key";
            orderResponse.RazorpayOrderId = mockOrderId;
            orderResponse.IsSimulator = true;
        }
        else
        {
            try
            {
                var client = new RazorpayClient(keyId, keySecret);
                var options = new Dictionary<string, object>
                {
                    { "amount", Convert.ToInt32(finalAmount * 100) }, // amount in paisa
                    { "currency", "INR" },
                    { "receipt", localOrder.Id.ToString() }
                };

                var rzpOrder = client.Order.Create(options);
                var razorpayOrderId = rzpOrder["id"].ToString() ?? string.Empty;

                localOrder.RazorpayOrderId = razorpayOrderId;
                await _context.SaveChangesAsync();

                orderResponse.RazorpayKey = keyId!;
                orderResponse.RazorpayOrderId = razorpayOrderId;
                orderResponse.IsSimulator = false;
            }
            catch (Exception ex)
            {
                return ErrorResponse($"Failed to contact payment gateway: {ex.Message}", 500);
            }
        }

        return SuccessResponse(orderResponse, "Order initialized successfully.");
    }

    [HttpPost("verify-payment")]
    public async Task<IActionResult> VerifyPayment([FromBody] PaymentVerifyRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.RazorpayOrderId))
        {
            return ErrorResponse("Invalid verification request.", 400);
        }

        var order = await _context.Orders.FirstOrDefaultAsync(o => o.RazorpayOrderId == request.RazorpayOrderId);
        if (order == null)
        {
            return NotFoundResponse("Order record not found.");
        }

        if (order.PaymentStatus == PaymentStatus.Success)
        {
            var verifiedResponse = new PaymentVerifyResponse { OrderId = order.Id };
            return SuccessResponse(verifiedResponse, "Payment already verified successfully.");
        }

        var keyId = _configuration["Razorpay:KeyId"];
        var keySecret = _configuration["Razorpay:KeySecret"];

        var isSimulator = string.IsNullOrEmpty(keyId) || 
                          string.IsNullOrEmpty(keySecret) || 
                          keyId.StartsWith("YOUR_", StringComparison.OrdinalIgnoreCase);

        bool isValidSignature = false;

        if (isSimulator)
        {
            isValidSignature = request.RazorpaySignature == "simulated_signature";
        }
        else
        {
            try
            {
                var payload = new Dictionary<string, string>
                {
                    { "razorpay_order_id", request.RazorpayOrderId },
                    { "razorpay_payment_id", request.RazorpayPaymentId },
                    { "razorpay_signature", request.RazorpaySignature }
                };

                // Verify signature using Razorpay SDK
                Utils.verifyPaymentSignature(payload);
                isValidSignature = true;
            }
            catch (Exception)
            {
                isValidSignature = false;
            }
        }

        if (isValidSignature)
        {
            order.PaymentStatus = PaymentStatus.Success;
            order.RazorpayPaymentId = request.RazorpayPaymentId;
            
            // Increment coupon usage count if used
            if (!string.IsNullOrEmpty(order.CouponCode))
            {
                var coupon = await _context.Coupons.FirstOrDefaultAsync(c => c.Code.ToUpper() == order.CouponCode.ToUpper());
                if (coupon != null)
                {
                    coupon.UsedCount++;
                }
            }

            await _context.SaveChangesAsync();

            // Queue post-payment background automation via Hangfire
            BackgroundJob.Enqueue<IEmailService>(emailService => 
                emailService.SendPurchaseConfirmationEmailAsync(order.Id));
            
            BackgroundJob.Enqueue<IEmailService>(emailService => 
                emailService.SendAdminNotificationEmailAsync(order.Id));

            var verifyResponse = new PaymentVerifyResponse { OrderId = order.Id };
            return SuccessResponse(verifyResponse, "Payment verified and enrolment completed.");
        }
        else
        {
            order.PaymentStatus = PaymentStatus.Failed;
            await _context.SaveChangesAsync();
            return ErrorResponse("Payment signature verification failed.", 400);
        }
    }

    [HttpPost("resend-details")]
    public async Task<IActionResult> ResendDetails([FromBody] ResendDetailsRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Email))
        {
            return ErrorResponse("Email address is required.", 400);
        }

        var emailNormalized = request.Email.Trim().ToUpperInvariant();
        var order = await _context.Orders
            .Where(o => o.CourseId == request.CourseId && 
                        o.CustomerEmail.ToUpper() == emailNormalized && 
                        o.PaymentStatus == PaymentStatus.Success)
            .OrderByDescending(o => o.CreatedAt)
            .FirstOrDefaultAsync();

        if (order == null)
        {
            return NotFoundResponse("No successful enrollment was found for this email address.");
        }

        // Queue resending the kit via Hangfire
        BackgroundJob.Enqueue<IEmailService>(emailService => 
            emailService.SendPurchaseConfirmationEmailAsync(order.Id));

        return SuccessResponse<object>(null!, "Enrolment details have been queued for email delivery.");
    }

    [HttpPost("webhook")]
    [Route("/api/webhooks/razorpay")]
    public async Task<IActionResult> RazorpayWebhook()
    {
        using var reader = new System.IO.StreamReader(Request.Body);
        var json = await reader.ReadToEndAsync();

        var webhookSecret = _configuration["Razorpay:WebhookSecret"];
        var expectedSignature = Request.Headers["X-Razorpay-Signature"].ToString();

        if (string.IsNullOrEmpty(webhookSecret) || string.IsNullOrEmpty(expectedSignature))
        {
            return BadRequest("Webhook credentials are not configured.");
        }

        try
        {
            Utils.verifyWebhookSignature(json, expectedSignature, webhookSecret);

            using var doc = System.Text.Json.JsonDocument.Parse(json);
            var root = doc.RootElement;
            
            var eventName = root.GetProperty("event").GetString();
            if (eventName == "order.paid" || eventName == "payment.captured")
            {
                var payloadElement = root.GetProperty("payload");
                var entityElement = eventName == "order.paid" 
                    ? payloadElement.GetProperty("order").GetProperty("entity") 
                    : payloadElement.GetProperty("payment").GetProperty("entity");

                var razorpayOrderId = entityElement.GetProperty("id").GetString();
                if (eventName == "payment.captured")
                {
                    razorpayOrderId = entityElement.GetProperty("order_id").GetString();
                }

                var razorpayPaymentId = eventName == "payment.captured" 
                    ? entityElement.GetProperty("id").GetString() 
                    : null;

                if (!string.IsNullOrEmpty(razorpayOrderId))
                {
                    var order = await _context.Orders.FirstOrDefaultAsync(o => o.RazorpayOrderId == razorpayOrderId);
                    if (order != null && order.PaymentStatus != PaymentStatus.Success)
                    {
                        order.PaymentStatus = PaymentStatus.Success;
                        if (!string.IsNullOrEmpty(razorpayPaymentId))
                        {
                            order.RazorpayPaymentId = razorpayPaymentId;
                        }

                        if (!string.IsNullOrEmpty(order.CouponCode))
                        {
                            var coupon = await _context.Coupons.FirstOrDefaultAsync(c => c.Code.ToUpper() == order.CouponCode.ToUpper());
                            if (coupon != null)
                            {
                                coupon.UsedCount++;
                            }
                        }

                        await _context.SaveChangesAsync();

                        BackgroundJob.Enqueue<IEmailService>(emailService => 
                            emailService.SendPurchaseConfirmationEmailAsync(order.Id));
                        
                        BackgroundJob.Enqueue<IEmailService>(emailService => 
                            emailService.SendAdminNotificationEmailAsync(order.Id));
                    }
                }
            }
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }

        return Ok();
    }
}
