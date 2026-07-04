using CollegeLMS.API.Data;
using CollegeLMS.API.Dtos;
using CollegeLMS.API.Entities;
using CollegeLMS.API.Interfaces;
using CollegeLMS.API.Mappers;
using CollegeLMS.API.Response;
using Microsoft.EntityFrameworkCore;

namespace CollegeLMS.API.Services;

public class NewsService(AppDbContext db) : INewsService
{
    public async Task<Result<PagedResponse<NewsResponse>>> GetAllAsync(
        int page,
        int pageSize,
        Guid? categoryId,
        string? search,
        CancellationToken ct
    )
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = db
            .News.AsNoTracking()
            .Include(n => n.Category)
            .Include(n => n.CreatedBy)
            .AsQueryable();

        if (categoryId.HasValue)
            query = query.Where(n => n.CategoryId == categoryId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.ToLower();
            query = query.Where(n =>
                n.Title.ToLower().Contains(term) || n.Content.ToLower().Contains(term)
            );
        }

        var totalCount = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(n => n.PublishedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(n => n.ToDto())
            .ToListAsync(ct);

        return Result<PagedResponse<NewsResponse>>.Ok(
            new PagedResponse<NewsResponse>(items, totalCount, page, pageSize)
        );
    }

    public async Task<Result<NewsResponse>> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var news = await db
            .News.AsNoTracking()
            .Include(n => n.Category)
            .Include(n => n.CreatedBy)
            .FirstOrDefaultAsync(n => n.Id == id, ct);

        if (news is null)
            return Result<NewsResponse>.Fail("Новость не найдена", 404);

        return Result<NewsResponse>.Ok(news.ToDto());
    }

    public async Task<Result<NewsResponse>> CreateAsync(
        CreateNewsRequest request,
        Guid currentUserId,
        CancellationToken ct
    )
    {
        if (request.CategoryId.HasValue)
        {
            var categoryExists = await db.NewsCategories.AnyAsync(
                c => c.Id == request.CategoryId,
                ct
            );
            if (!categoryExists)
                return Result<NewsResponse>.Fail("Категория не найдена", 404);
        }

        var news = new News
        {
            Id = Guid.NewGuid(),
            Title = request.Title,
            Content = request.Content,
            ImageUrl = request.ImageUrl,
            CategoryId = request.CategoryId,
            IsPublished = request.IsPublished,
            PublishedAt = request.PublishedAt ?? DateTime.UtcNow,
            CreatedById = currentUserId,
        };

        db.News.Add(news);
        await db.SaveChangesAsync(ct);

        var saved = await db
            .News.Include(n => n.Category)
            .Include(n => n.CreatedBy)
            .FirstOrDefaultAsync(n => n.Id == news.Id, ct);

        return Result<NewsResponse>.Ok((saved ?? news).ToDto());
    }

    public async Task<Result<NewsResponse>> UpdateAsync(
        Guid id,
        UpdateNewsRequest request,
        CancellationToken ct
    )
    {
        var news = await db
            .News.Include(n => n.Category)
            .Include(n => n.CreatedBy)
            .FirstOrDefaultAsync(n => n.Id == id, ct);

        if (news is null)
            return Result<NewsResponse>.Fail("Новость не найдена", 404);

        if (request.CategoryId.HasValue)
        {
            var categoryExists = await db.NewsCategories.AnyAsync(
                c => c.Id == request.CategoryId,
                ct
            );
            if (!categoryExists)
                return Result<NewsResponse>.Fail("Категория не найдена", 404);
        }

        news.Title = request.Title;
        news.Content = request.Content;
        news.ImageUrl = request.ImageUrl;
        news.CategoryId = request.CategoryId;
        news.IsPublished = request.IsPublished;
        news.PublishedAt = request.PublishedAt ?? news.PublishedAt;
        news.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        return Result<NewsResponse>.Ok(news.ToDto());
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken ct)
    {
        var news = await db.News.FirstOrDefaultAsync(n => n.Id == id, ct);

        if (news is null)
            return Result.Fail("Новость не найдена", 404);

        news.IsDeleted = true;
        news.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        return Result.Ok();
    }

    public async Task<Result<List<NewsCategoryResponse>>> GetCategoriesAsync(CancellationToken ct)
    {
        var categories = await db
            .NewsCategories.AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => c.ToDto())
            .ToListAsync(ct);

        return Result<List<NewsCategoryResponse>>.Ok(categories);
    }
}
