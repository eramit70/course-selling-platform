using System.Diagnostics;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TradingCourse.Application;
using TradingCourse.Application.Models;
using TradingCourse.Application.Services;
using TradingCourse.Web.Models;

namespace TradingCourse.Web.Controllers;

public class HomeController : Controller
{
    private readonly AppDbContext _context;
    private readonly ICourseService _courseService;
    private readonly IHomepageBannerService _bannerService;
    private readonly ILogger<HomeController> _logger;

    public HomeController(AppDbContext context, ICourseService courseService, IHomepageBannerService bannerService, ILogger<HomeController> logger)
    {
        _context = context;
        _courseService = courseService;
        _bannerService = bannerService;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        var featuredCourse = await _courseService.GetFeaturedCourseAsync();
        var activeBanners = (await _bannerService.GetActiveBannersAsync()).ToList();
        var courses = (await _courseService.GetActiveLiveCoursesAsync()).Take(8).ToList();

        var viewModel = new HomeIndexViewModel
        {
            FeaturedCourse = featuredCourse,
            Banners = activeBanners,
            Courses = courses
        };

        return View(viewModel);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
