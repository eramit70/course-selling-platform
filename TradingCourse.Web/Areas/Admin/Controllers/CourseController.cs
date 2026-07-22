using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TradingCourse.Application.Models;
using TradingCourse.Application.Services;

namespace TradingCourse.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class CourseController : Controller
{
    private readonly ICourseService _courseService;
    private readonly IWebHostEnvironment _env;

    public CourseController(ICourseService courseService, IWebHostEnvironment env)
    {
        _courseService = courseService;
        _env = env;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var courses = await _courseService.GetAllCoursesAsync();
        return View(courses);
    }

    [HttpGet]
    public IActionResult Create()
    {
        var defaultCourse = new Course
        {
            Title = "Price Action Masterclass",
            Slug = "price-action-masterclass",
            Instructor = "Amit Sharma",
            Schedule = "Sat & Sun, 7:00 PM IST",
            PurchaseDurationDays = 365,
            Price = 9999.00m,
            SalePrice = 4999.00m,
            IsLive = true,
            ShortDescription = "Master the art of Price Action trading, advanced chart analysis, and mathematical risk blueprints with real-time live trading cohorts.",
            FullDescription = "<h3>Course Syllabus</h3>\n<p><strong>Module 1: Market Structure</strong><br/>Understanding supply and demand zones.</p>\n<p><strong>Module 2: Advanced Price Action</strong><br/>Trading without indicators using pure price.</p>\n<p><strong>Module 3: Risk Management</strong><br/>Protecting capital and position sizing.</p>",
            EmailTemplateSubject = "Welcome to {CourseTitle}! - Enrollment Confirmed",
            EmailTemplateBody = "<p>Hi {CustomerName},</p>\n<p>Welcome to <strong>{CourseTitle}</strong>!</p>\n<p>Your class schedule: {Schedule}</p>\n<p>Join live classes here: <a href=\"{LiveClassLink}\">{LiveClassLink}</a></p>\n<br/><p>Regards,<br/>The GrowLog Team</p>"
        };
        return View(defaultCourse);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Course course, IFormFile? courseMediaFile)
    {
        if (course.MediaType == "Image" || course.MediaType == "Video")
        {
            if (courseMediaFile != null && courseMediaFile.Length > 0)
            {
                ValidateUploadedFile(course.MediaType, courseMediaFile);
            }
            else
            {
                ModelState.AddModelError("courseMediaFile", "Media file is required when type is Image or Video.");
            }
        }

        if (ModelState.IsValid)
        {
            if (courseMediaFile != null && courseMediaFile.Length > 0)
            {
                course.MediaUrl = await SaveUploadedFileAsync(courseMediaFile, course.MediaType);
            }
            else if (course.MediaType != "YouTube")
            {
                course.MediaUrl = null;
            }

            await _courseService.CreateCourseAsync(course);
            TempData["SuccessMessage"] = $"Course '{course.Title}' Created Successfully";
            return RedirectToAction(nameof(Index), "Course", new { area = "Admin" });
        }
        return View(course);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var course = await _courseService.GetCourseByIdAsync(id);
        if (course == null) return NotFound();
        return View(course);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Course course, IFormFile? courseMediaFile)
    {
        var existingCourse = await _courseService.GetCourseByIdAsync(course.Id);
        if (existingCourse == null) return NotFound();

        if (course.MediaType == "Image" || course.MediaType == "Video")
        {
            if (courseMediaFile != null && courseMediaFile.Length > 0)
            {
                ValidateUploadedFile(course.MediaType, courseMediaFile);
            }
            else if (string.IsNullOrEmpty(existingCourse.MediaUrl) || existingCourse.MediaType != course.MediaType)
            {
                ModelState.AddModelError("courseMediaFile", "Media file is required.");
            }
        }

        if (ModelState.IsValid)
        {
            if (course.MediaType == "Image" || course.MediaType == "Video")
            {
                if (courseMediaFile != null && courseMediaFile.Length > 0)
                {
                    course.MediaUrl = await SaveUploadedFileAsync(courseMediaFile, course.MediaType);
                }
                else
                {
                    course.MediaUrl = existingCourse.MediaUrl;
                }
            }
            else if (course.MediaType == "YouTube")
            {
                // MediaUrl is bound via the text input
            }
            else
            {
                course.MediaUrl = null;
            }

            // Transfer updated details
            existingCourse.Title = course.Title;
            existingCourse.Slug = course.Slug;
            existingCourse.Instructor = course.Instructor;
            existingCourse.Schedule = course.Schedule;
            existingCourse.PurchaseDurationDays = course.PurchaseDurationDays;
            existingCourse.Price = course.Price;
            existingCourse.SalePrice = course.SalePrice;
            existingCourse.LiveClassLink = course.LiveClassLink;
            existingCourse.IsLive = course.IsLive;
            existingCourse.MediaType = course.MediaType;
            existingCourse.MediaUrl = course.MediaUrl;
            existingCourse.ShortDescription = course.ShortDescription;
            existingCourse.FullDescription = course.FullDescription;
            existingCourse.EmailTemplateSubject = course.EmailTemplateSubject;
            existingCourse.EmailTemplateBody = course.EmailTemplateBody;

            await _courseService.UpdateCourseAsync(existingCourse);
            TempData["SuccessMessage"] = $"Course '{existingCourse.Title}' Updated Successfully";
            return RedirectToAction(nameof(Index), "Course", new { area = "Admin" });
        }
        return View(course);
    }

    private void ValidateUploadedFile(string mediaType, IFormFile file)
    {
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (mediaType == "Image")
        {
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            if (!allowedExtensions.Contains(extension))
            {
                ModelState.AddModelError("courseMediaFile", "Invalid image format. Allowed formats: JPG, JPEG, PNG, WEBP.");
            }
            if (file.Length > 3 * 1024 * 1024) // 3 MB
            {
                ModelState.AddModelError("courseMediaFile", "Image size exceeds 3 MB limit.");
            }
        }
        else if (mediaType == "Video")
        {
            var allowedExtensions = new[] { ".mp4", ".webm" };
            if (!allowedExtensions.Contains(extension))
            {
                ModelState.AddModelError("courseMediaFile", "Invalid video format. Allowed formats: MP4, WEBM.");
            }
            if (file.Length > 10 * 1024 * 1024) // 10 MB
            {
                ModelState.AddModelError("courseMediaFile", "Video size exceeds 10 MB limit.");
            }
        }
    }

    private async Task<string> SaveUploadedFileAsync(IFormFile file, string mediaType)
    {
        string subFolder = mediaType == "Video" ? "videos" : "courses";
        var uploadsDir = Path.Combine(_env.WebRootPath, "uploads", subFolder);
        if (!Directory.Exists(uploadsDir))
        {
            Directory.CreateDirectory(uploadsDir);
        }

        var uniqueName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
        var filePath = Path.Combine(uploadsDir, uniqueName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        return $"/uploads/{subFolder}/" + uniqueName;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var course = await _courseService.GetCourseByIdAsync(id);
        string courseTitle = course?.Title ?? "Course";
        
        try
        {
            await _courseService.DeleteCourseAsync(id);
            TempData["SuccessMessage"] = $"Course '{courseTitle}' Deleted Successfully";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Cannot delete course '{courseTitle}' because it is in use by another transaction (e.g. enrolments/leads).";
        }
        
        return RedirectToAction(nameof(Index), "Course", new { area = "Admin" });
    }
}
