using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TradingCourse.Application.Models;

namespace TradingCourse.Application.Services;

public class HomepageBannerService : IHomepageBannerService
{
    private readonly AppDbContext _context;

    public HomepageBannerService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<HomepageBanner>> GetAllBannersAsync()
    {
        return await _context.HomepageBanners
            .OrderBy(b => b.DisplayOrder)
            .ThenByDescending(b => b.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<HomepageBanner>> GetActiveBannersAsync()
    {
        return await _context.HomepageBanners
            .Where(b => b.IsActive)
            .OrderBy(b => b.DisplayOrder)
            .ThenByDescending(b => b.CreatedAt)
            .ToListAsync();
    }

    public async Task<HomepageBanner?> GetBannerByIdAsync(int id)
    {
        return await _context.HomepageBanners.FindAsync(id);
    }

    public async Task<bool> CreateBannerAsync(HomepageBanner banner)
    {
        banner.CreatedAt = DateTime.UtcNow;
        banner.UpdatedAt = DateTime.UtcNow;
        _context.HomepageBanners.Add(banner);
        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<bool> UpdateBannerAsync(HomepageBanner banner)
    {
        banner.UpdatedAt = DateTime.UtcNow;
        _context.Entry(banner).Property(x => x.CreatedAt).IsModified = false;
        _context.HomepageBanners.Update(banner);
        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<bool> DeleteBannerAsync(int id)
    {
        var banner = await _context.HomepageBanners.FindAsync(id);
        if (banner == null) return false;

        _context.HomepageBanners.Remove(banner);
        return await _context.SaveChangesAsync() > 0;
    }
}
