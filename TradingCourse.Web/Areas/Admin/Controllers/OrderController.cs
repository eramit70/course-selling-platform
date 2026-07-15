using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TradingCourse.Application;
using TradingCourse.Application.Models;

namespace TradingCourse.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class OrderController : Controller
{
    private readonly AppDbContext _context;

    public OrderController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int? courseId, PaymentStatus? status, string? search)
    {
        var query = _context.Orders
            .Include(o => o.Course)
            .OrderByDescending(o => o.CreatedAt)
            .AsQueryable();

        if (courseId.HasValue)
        {
            query = query.Where(o => o.CourseId == courseId.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(o => o.PaymentStatus == status.Value);
        }

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(o => o.CustomerName.Contains(search) || 
                                     o.CustomerEmail.Contains(search) || 
                                     o.CustomerMobile.Contains(search));
        }

        var orders = await query.ToListAsync();

        var courses = await _context.Courses.ToListAsync();
        ViewBag.Courses = new SelectList(courses, "Id", "Title", courseId);
        ViewBag.SelectedStatus = status;
        ViewBag.SearchQuery = search;
        ViewBag.SelectedCourseId = courseId;

        return View(orders);
    }

    [HttpGet]
    public async Task<IActionResult> ExportToCsv(int? courseId, PaymentStatus? status, string? search)
    {
        var query = _context.Orders
            .Include(o => o.Course)
            .OrderByDescending(o => o.CreatedAt)
            .AsQueryable();

        if (courseId.HasValue)
        {
            query = query.Where(o => o.CourseId == courseId.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(o => o.PaymentStatus == status.Value);
        }

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(o => o.CustomerName.Contains(search) || 
                                     o.CustomerEmail.Contains(search) || 
                                     o.CustomerMobile.Contains(search));
        }

        var orders = await query.ToListAsync();

        var builder = new StringBuilder();
        builder.AppendLine("OrderId,CustomerName,CustomerEmail,CustomerMobile,CourseTitle,AmountPaid,CouponCode,PaymentStatus,RazorpayOrderId,RazorpayPaymentId,CreatedAt");

        foreach (var order in orders)
        {
            builder.AppendLine($"\"{order.Id}\",\"{order.CustomerName.Replace("\"", "\"\"")}\",\"{order.CustomerEmail.Replace("\"", "\"\"")}\",\"{order.CustomerMobile}\",\"{order.Course?.Title.Replace("\"", "\"\"")}\",\"{order.AmountPaid}\",\"{order.CouponCode}\",\"{order.PaymentStatus}\",\"{order.RazorpayOrderId}\",\"{order.RazorpayPaymentId}\",\"{order.CreatedAt.ToLocalTime():yyyy-MM-dd HH:mm:ss}\"");
        }

        var bytes = Encoding.UTF8.GetBytes(builder.ToString());
        return File(bytes, "text/csv", $"orders_leads_export_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv");
    }
}
