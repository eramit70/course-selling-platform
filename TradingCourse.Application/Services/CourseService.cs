using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TradingCourse.Application.Models;

namespace TradingCourse.Application.Services;

public class CourseService : ICourseService
{
    private readonly AppDbContext _context;

    public CourseService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Course>> GetAllCoursesAsync()
    {
        return await _context.Courses
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Course>> GetActiveLiveCoursesAsync()
    {
        return await _context.Courses
            .Where(c => c.IsLive)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();
    }

    public async Task<Course?> GetCourseByIdAsync(int id)
    {
        return await _context.Courses.FindAsync(id);
    }

    public async Task<Course?> GetCourseBySlugAsync(string slug)
    {
        return await _context.Courses
            .FirstOrDefaultAsync(c => c.Slug == slug);
    }

    public async Task<Course?> GetFeaturedCourseAsync()
    {
        // Get the most recent live course as the featured/landing page course
        return await _context.Courses
            .Where(c => c.IsLive)
            .OrderByDescending(c => c.CreatedAt)
            .FirstOrDefaultAsync();
    }

    public async Task<bool> CreateCourseAsync(Course course)
    {
        course.CreatedAt = DateTime.UtcNow;
        course.UpdatedAt = DateTime.UtcNow;
        _context.Courses.Add(course);
        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<bool> UpdateCourseAsync(Course course)
    {
        course.UpdatedAt = DateTime.UtcNow;
        // Keep the original CreatedAt date if it exists
        _context.Entry(course).Property(x => x.CreatedAt).IsModified = false;
        _context.Courses.Update(course);
        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<bool> DeleteCourseAsync(int id)
    {
        var course = await _context.Courses.FindAsync(id);
        if (course == null) return false;

        _context.Courses.Remove(course);
        return await _context.SaveChangesAsync() > 0;
    }
}
