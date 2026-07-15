using System.Collections.Generic;
using System.Threading.Tasks;
using TradingCourse.Application.Models;

namespace TradingCourse.Application.Services;

public interface ICourseService
{
    Task<IEnumerable<Course>> GetAllCoursesAsync();
    Task<IEnumerable<Course>> GetActiveLiveCoursesAsync();
    Task<Course?> GetCourseByIdAsync(int id);
    Task<Course?> GetCourseBySlugAsync(string slug);
    Task<Course?> GetFeaturedCourseAsync();
    Task<bool> CreateCourseAsync(Course course);
    Task<bool> UpdateCourseAsync(Course course);
    Task<bool> DeleteCourseAsync(int id);
}
