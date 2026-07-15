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
public class HomepageBannerController : Controller
{
    private readonly IHomepageBannerService _bannerService;
    private readonly IWebHostEnvironment _env;

    public HomepageBannerController(IHomepageBannerService bannerService, IWebHostEnvironment env)
    {
        _bannerService = bannerService;
        _env = env;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var banners = await _bannerService.GetAllBannersAsync();
        return View(banners);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new HomepageBanner());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(HomepageBanner banner, IFormFile? mediaFile)
    {
        ModelState.Remove(nameof(HomepageBanner.MediaUrl));

        if (!string.IsNullOrEmpty(banner.Heading) && banner.Heading.Length > 100)
        {
            ModelState.AddModelError(nameof(HomepageBanner.Heading), "Heading cannot exceed 100 characters.");
        }
        if (!string.IsNullOrEmpty(banner.Subheading) && banner.Subheading.Length > 250)
        {
            ModelState.AddModelError(nameof(HomepageBanner.Subheading), "Subheading cannot exceed 250 characters.");
        }

        if (mediaFile == null || mediaFile.Length == 0)
        {
            ModelState.AddModelError("mediaFile", "Media file is required.");
        }
        else
        {
            ValidateUploadedFile(banner.MediaType, mediaFile);
        }

        if (ModelState.IsValid && mediaFile != null)
        {
            banner.MediaUrl = await SaveUploadedFileAsync(mediaFile);
            await _bannerService.CreateBannerAsync(banner);
            TempData["SuccessMessage"] = $"Slide '{banner.Heading}' Created Successfully";
            return RedirectToAction(nameof(Index), "HomepageBanner", new { area = "Admin" });
        }

        return View(banner);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var banner = await _bannerService.GetBannerByIdAsync(id);
        if (banner == null) return NotFound();
        return View(banner);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(HomepageBanner banner, IFormFile? mediaFile)
    {
        ModelState.Remove(nameof(HomepageBanner.MediaUrl));

        if (!string.IsNullOrEmpty(banner.Heading) && banner.Heading.Length > 100)
        {
            ModelState.AddModelError(nameof(HomepageBanner.Heading), "Heading cannot exceed 100 characters.");
        }
        if (!string.IsNullOrEmpty(banner.Subheading) && banner.Subheading.Length > 250)
        {
            ModelState.AddModelError(nameof(HomepageBanner.Subheading), "Subheading cannot exceed 250 characters.");
        }

        var existingBanner = await _bannerService.GetBannerByIdAsync(banner.Id);
        if (existingBanner == null) return NotFound();

        if (mediaFile != null && mediaFile.Length > 0)
        {
            ValidateUploadedFile(banner.MediaType, mediaFile);
        }

        if (ModelState.IsValid)
        {
            if (mediaFile != null && mediaFile.Length > 0)
            {
                banner.MediaUrl = await SaveUploadedFileAsync(mediaFile);
            }
            else
            {
                banner.MediaUrl = existingBanner.MediaUrl;
            }

            // Manually transfer properties to keep change tracking happy
            existingBanner.Heading = banner.Heading;
            existingBanner.Subheading = banner.Subheading;
            existingBanner.MediaType = banner.MediaType;
            existingBanner.MediaUrl = banner.MediaUrl;
            existingBanner.ButtonText = banner.ButtonText;
            existingBanner.ButtonUrl = banner.ButtonUrl;
            existingBanner.DisplayOrder = banner.DisplayOrder;
            existingBanner.IsActive = banner.IsActive;

            await _bannerService.UpdateBannerAsync(existingBanner);
            TempData["SuccessMessage"] = $"Slide '{existingBanner.Heading}' Updated Successfully";
            return RedirectToAction(nameof(Index), "HomepageBanner", new { area = "Admin" });
        }

        return View(banner);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var banner = await _bannerService.GetBannerByIdAsync(id);
        string bannerHeading = banner?.Heading ?? "Slide";
        
        try
        {
            await _bannerService.DeleteBannerAsync(id);
            TempData["SuccessMessage"] = $"Slide '{bannerHeading}' Deleted Successfully";
        }
        catch (System.Exception)
        {
            TempData["ErrorMessage"] = $"Cannot delete slide '{bannerHeading}' due to a database dependency.";
        }
        
        return RedirectToAction(nameof(Index), "HomepageBanner", new { area = "Admin" });
    }

    private void ValidateUploadedFile(string mediaType, IFormFile file)
    {
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        
        if (mediaType == "Photo")
        {
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            if (!allowedExtensions.Contains(extension))
            {
                ModelState.AddModelError("mediaFile", "Invalid image format. Allowed formats: JPG, JPEG, PNG, WEBP.");
            }
            if (file.Length > 3 * 1024 * 1024) // 3 MB
            {
                ModelState.AddModelError("mediaFile", "Image file size exceeds the 3 MB limit.");
            }
        }
        else if (mediaType == "Video")
        {
            var allowedExtensions = new[] { ".mp4", ".webm" };
            if (!allowedExtensions.Contains(extension))
            {
                ModelState.AddModelError("mediaFile", "Invalid video format. Allowed formats: MP4, WEBM.");
            }
            if (file.Length > 10 * 1024 * 1024) // 10 MB
            {
                ModelState.AddModelError("mediaFile", "Video file size exceeds the 10 MB limit.");
            }
        }
    }

    private async Task<string> SaveUploadedFileAsync(IFormFile file)
    {
        var uploadsDir = Path.Combine(_env.WebRootPath, "uploads");
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

        return "/uploads/" + uniqueName;
    }
}
