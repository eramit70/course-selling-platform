using System.Collections.Generic;
using System.Threading.Tasks;
using TradingCourse.Application.Models;

namespace TradingCourse.Application.Services;

public interface ICouponService
{
    Task<IEnumerable<Coupon>> GetAllCouponsAsync();
    Task<Coupon?> GetCouponByIdAsync(int id);
    Task<Coupon?> GetCouponByCodeAsync(string code);
    Task<bool> CreateCouponAsync(Coupon coupon);
    Task<bool> UpdateCouponAsync(Coupon coupon);
    Task<bool> DeleteCouponAsync(int id);
    Task<(bool IsValid, string Message)> ValidateCouponAsync(string code, int courseId, decimal orderAmount);
    Task<decimal> CalculateDiscountAsync(string code, int courseId, decimal orderAmount);
}
