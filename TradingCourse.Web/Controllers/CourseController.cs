using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TradingCourse.Application;
using TradingCourse.Application.Services;

namespace TradingCourse.Web.Controllers;

public class CourseController : Controller
{
    private readonly ICourseService _courseService;
    private readonly AppDbContext _context;

    public CourseController(ICourseService courseService, AppDbContext context)
    {
        _courseService = courseService;
        _context = context;
    }

    [HttpGet("courses")]
    public async Task<IActionResult> Index()
    {
        var courses = await _courseService.GetActiveLiveCoursesAsync();
        return View(courses);
    }

    [HttpGet("courses/{slug}")]
    public async Task<IActionResult> Details(string slug)
    {
        if (string.IsNullOrEmpty(slug)) return NotFound();

        var course = await _courseService.GetCourseBySlugAsync(slug);
        if (course == null) return NotFound();

        return View(course);
    }

    [HttpGet("courses/{slug}/checkout")]
    public async Task<IActionResult> Checkout(string slug)
    {
        if (string.IsNullOrEmpty(slug)) return NotFound();

        var course = await _courseService.GetCourseBySlugAsync(slug);
        if (course == null || !course.IsLive) return NotFound();

        return View(course);
    }

    [HttpGet("courses/success")]
    public async Task<IActionResult> Success(int orderId)
    {
        var order = await _context.Orders
            .Include(o => o.Course)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order == null) return NotFound();

        return View(order);
    }
}
