using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TradingCourse.Application.Models;

namespace TradingCourse.Application.Services;

public class CouponService : ICouponService
{
    private readonly AppDbContext _context;

    public CouponService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Coupon>> GetAllCouponsAsync()
    {
        return await _context.Coupons
            .Include(c => c.Course)
            .OrderByDescending(c => c.Id)
            .ToListAsync();
    }

    public async Task<Coupon?> GetCouponByIdAsync(int id)
    {
        return await _context.Coupons
            .Include(c => c.Course)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<Coupon?> GetCouponByCodeAsync(string code)
    {
        if (string.IsNullOrWhiteSpace(code)) return null;

        var normalizedCode = code.Trim().ToUpperInvariant();
        return await _context.Coupons
            .Include(c => c.Course)
            .FirstOrDefaultAsync(c => c.Code.ToUpper() == normalizedCode);
    }

    public async Task<bool> CreateCouponAsync(Coupon coupon)
    {
        coupon.Code = coupon.Code.Trim().ToUpperInvariant();
        _context.Coupons.Add(coupon);
        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<bool> UpdateCouponAsync(Coupon coupon)
    {
        coupon.Code = coupon.Code.Trim().ToUpperInvariant();
        _context.Coupons.Update(coupon);
        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<bool> DeleteCouponAsync(int id)
    {
        var coupon = await _context.Coupons.FindAsync(id);
        if (coupon == null) return false;

        _context.Coupons.Remove(coupon);
        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<(bool IsValid, string Message)> ValidateCouponAsync(string code, int courseId, decimal orderAmount)
    {
        var coupon = await GetCouponByCodeAsync(code);
        if (coupon == null)
        {
            return (false, "Invalid coupon code.");
        }

        if (coupon.ExpiryDate.HasValue && coupon.ExpiryDate.Value < DateTime.UtcNow)
        {
            return (false, "Coupon has expired.");
        }

        if (coupon.MaxUsage.HasValue && coupon.UsedCount >= coupon.MaxUsage.Value)
        {
            return (false, "Coupon usage limit reached.");
        }

        if (coupon.MinPurchaseAmount.HasValue && orderAmount < coupon.MinPurchaseAmount.Value)
        {
            return (false, $"Minimum purchase of {coupon.MinPurchaseAmount.Value} required to use this coupon.");
        }

        if (coupon.CourseId.HasValue && coupon.CourseId.Value != courseId)
        {
            return (false, "This coupon is not applicable to the selected course.");
        }

        return (true, "Coupon applied successfully.");
    }

    public async Task<decimal> CalculateDiscountAsync(string code, int courseId, decimal orderAmount)
    {
        var validation = await ValidateCouponAsync(code, courseId, orderAmount);
        if (!validation.IsValid)
        {
            return 0;
        }

        var coupon = await GetCouponByCodeAsync(code);
        if (coupon == null) return 0;

        decimal discount = 0;
        if (coupon.DiscountType == DiscountType.Percentage)
        {
            discount = orderAmount * (coupon.DiscountValue / 100m);
        }
        else if (coupon.DiscountType == DiscountType.Fixed)
        {
            discount = coupon.DiscountValue;
        }

        // Cap discount to orderAmount to prevent negative prices
        if (discount > orderAmount)
        {
            discount = orderAmount;
        }

        return Math.Round(discount, 2);
    }
}
