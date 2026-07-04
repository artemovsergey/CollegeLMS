using CollegeLMS.API.Dtos;
using CollegeLMS.API.Entities;

namespace CollegeLMS.API.Mappers;

public static class NewsMapper
{
    public static NewsResponse ToDto(this News news) =>
        new()
        {
            Id = news.Id,
            Title = news.Title,
            Content = news.Content,
            ImageUrl = news.ImageUrl,
            CategoryId = news.CategoryId,
            CategoryName = news.Category?.Name ?? string.Empty,
            IsPublished = news.IsPublished,
            PublishedAt = news.PublishedAt,
            CreatedAt = news.CreatedAt,
            CreatedByName = news.CreatedBy?.FullName ?? string.Empty,
        };

    public static NewsCategoryResponse ToDto(this NewsCategory category) =>
        new()
        {
            Id = category.Id,
            Name = category.Name,
            Slug = category.Slug,
        };
}
