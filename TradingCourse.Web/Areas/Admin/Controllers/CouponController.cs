using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using TradingCourse.Application.Models;
using TradingCourse.Application.Services;

namespace TradingCourse.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class CouponController : Controller
{
    private readonly ICouponService _couponService;
    private readonly ICourseService _courseService;

    public CouponController(ICouponService couponService, ICourseService courseService)
    {
        _couponService = couponService;
        _courseService = courseService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var coupons = await _couponService.GetAllCouponsAsync();
        return View(coupons);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var courses = await _courseService.GetAllCoursesAsync();
        ViewBag.Courses = new SelectList(courses, "Id", "Title");
        return View(new Coupon());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Coupon coupon)
    {
        if (ModelState.IsValid)
        {
            await _couponService.CreateCouponAsync(coupon);
            TempData["SuccessMessage"] = $"Coupon '{coupon.Code}' Created Successfully";
            return RedirectToAction(nameof(Index), "Coupon", new { area = "Admin" });
        }
        var courses = await _courseService.GetAllCoursesAsync();
        ViewBag.Courses = new SelectList(courses, "Id", "Title", coupon.CourseId);
        return View(coupon);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var coupon = await _couponService.GetCouponByIdAsync(id);
        if (coupon == null) return NotFound();

        var courses = await _courseService.GetAllCoursesAsync();
        ViewBag.Courses = new SelectList(courses, "Id", "Title", coupon.CourseId);
        return View(coupon);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Coupon coupon)
    {
        if (ModelState.IsValid)
        {
            await _couponService.UpdateCouponAsync(coupon);
            TempData["SuccessMessage"] = $"Coupon '{coupon.Code}' Updated Successfully";
            return RedirectToAction(nameof(Index), "Coupon", new { area = "Admin" });
        }
        var courses = await _courseService.GetAllCoursesAsync();
        ViewBag.Courses = new SelectList(courses, "Id", "Title", coupon.CourseId);
        return View(coupon);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var coupon = await _couponService.GetCouponByIdAsync(id);
        string couponCode = coupon?.Code ?? "Coupon";
        
        try
        {
            await _couponService.DeleteCouponAsync(id);
            TempData["SuccessMessage"] = $"Coupon '{couponCode}' Deleted Successfully";
        }
        catch (System.Exception)
        {
            TempData["ErrorMessage"] = $"Cannot delete coupon '{couponCode}' because it is in use by another transaction.";
        }
        
        return RedirectToAction(nameof(Index), "Coupon", new { area = "Admin" });
    }
}
