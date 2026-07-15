using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TradingCourse.Application;
using TradingCourse.Application.Models;

namespace TradingCourse.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class HomeController : Controller
{
    private readonly AppDbContext _context;

    public HomeController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var totalCourses = await _context.Courses.CountAsync();
        var totalCoupons = await _context.Coupons.CountAsync();
        
        var successfulOrders = await _context.Orders
            .Where(o => o.PaymentStatus == PaymentStatus.Success)
            .ToListAsync();
            
        var totalOrders = successfulOrders.Count;
        var totalRevenue = successfulOrders.Sum(o => o.AmountPaid);

        var recentLeads = await _context.Orders
            .Include(o => o.Course)
            .OrderByDescending(o => o.CreatedAt)
            .Take(5)
            .ToListAsync();

        ViewBag.TotalCourses = totalCourses;
        ViewBag.TotalCoupons = totalCoupons;
        ViewBag.TotalOrders = totalOrders;
        ViewBag.TotalRevenue = totalRevenue;

        return View(recentLeads);
    }
}
