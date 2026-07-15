using System.Collections.Generic;
using TradingCourse.Application.Models;

namespace TradingCourse.Web.Models;

public class HomeIndexViewModel
{
    public Course? FeaturedCourse { get; set; }
    public IEnumerable<HomepageBanner> Banners { get; set; } = System.Array.Empty<HomepageBanner>();
    public IEnumerable<Course> Courses { get; set; } = System.Array.Empty<Course>();
}
