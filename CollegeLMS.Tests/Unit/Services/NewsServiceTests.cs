using CollegeLMS.API.Dtos;
using CollegeLMS.API.Entities;
using CollegeLMS.API.Entities.Enums;
using CollegeLMS.API.Services;
using CollegeLMS.Tests.Fixtures;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace CollegeLMS.Tests.Unit.Services;

public class NewsServiceTests : IDisposable
{
    private readonly API.Data.AppDbContext _db;
    private readonly NewsService _sut;
    private readonly Guid _adminId;

    public NewsServiceTests()
    {
        _db = TestDbContextFactory.Create();
        _sut = new NewsService(_db);
        _adminId = AddAdminUser();
    }

    public void Dispose() => _db.Dispose();

    private Guid AddAdminUser()
    {
        var id = Guid.NewGuid();
        _db.Users.Add(
            new User
            {
                Id = id,
                Email = "admin@test.ru",
                FullName = "Admin Test",
                PasswordHash = "hash",
                Role = UserRole.Admin,
                IsActive = true,
            }
        );
        _db.SaveChanges();
        return id;
    }

    [Fact]
    public async Task GetAllAsync_ReturnsEmptyList_WhenNoNews()
    {
        var result = await _sut.GetAllAsync(1, 10, null, null, default);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Items.Should().BeEmpty();
        result.Data.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsPagedNews()
    {
        var newsList = NewsFixture.CreateFaker().Generate(5);
        newsList.ForEach(n => n.CreatedById = _adminId);
        _db.News.AddRange(newsList);
        await _db.SaveChangesAsync();

        var result = await _sut.GetAllAsync(1, 10, null, null, default);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Items.Should().HaveCount(5);
        result.Data.TotalCount.Should().Be(5);
        result.Data.Page.Should().Be(1);
    }

    [Fact]
    public async Task GetAllAsync_RespectsPagination()
    {
        var newsList = NewsFixture.CreateFaker().Generate(10);
        newsList.ForEach(n => n.CreatedById = _adminId);
        _db.News.AddRange(newsList);
        await _db.SaveChangesAsync();

        var result = await _sut.GetAllAsync(1, 3, null, null, default);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Items.Should().HaveCount(3);
        result.Data.TotalCount.Should().Be(10);
        result.Data.TotalPages.Should().Be(4);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNews_WhenExists()
    {
        var news = NewsFixture.CreateFaker().Generate();
        news.CreatedById = _adminId;
        _db.News.Add(news);
        await _db.SaveChangesAsync();

        var result = await _sut.GetByIdAsync(news.Id, default);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Id.Should().Be(news.Id);
        result.Data.Title.Should().Be(news.Title);
    }

    [Fact]
    public async Task GetByIdAsync_Returns404_WhenNotFound()
    {
        var result = await _sut.GetByIdAsync(Guid.NewGuid(), default);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task CreateAsync_CreatesNews()
    {
        var request = new CreateNewsRequest
        {
            Title = "Тестовая новость",
            Content = "<p>Содержание</p>",
            IsPublished = true,
        };

        var result = await _sut.CreateAsync(request, _adminId, default);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Title.Should().Be("Тестовая новость");

        var saved = await _db.News.FirstAsync();
        saved.Title.Should().Be("Тестовая новость");
        saved.CreatedById.Should().Be(_adminId);
    }

    [Fact]
    public async Task UpdateAsync_UpdatesExistingNews()
    {
        var news = NewsFixture.CreateFaker().Generate();
        news.CreatedById = _adminId;
        _db.News.Add(news);
        await _db.SaveChangesAsync();

        var request = new UpdateNewsRequest
        {
            Title = "Обновлённый заголовок",
            Content = "<p>Обновлённое содержание</p>",
            IsPublished = false,
        };

        var result = await _sut.UpdateAsync(news.Id, request, default);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Title.Should().Be("Обновлённый заголовок");
        result.Data.IsPublished.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteAsync_SoftDeletes()
    {
        var news = NewsFixture.CreateFaker().Generate();
        news.CreatedById = _adminId;
        _db.News.Add(news);
        await _db.SaveChangesAsync();

        var result = await _sut.DeleteAsync(news.Id, default);

        result.IsSuccess.Should().BeTrue();

        var deleted = await _db.News.IgnoreQueryFilters().FirstAsync(n => n.Id == news.Id);
        deleted.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task GetAllAsync_FiltersByCategory()
    {
        var cat1 = new NewsCategory
        {
            Id = Guid.NewGuid(),
            Name = "Новости",
            Slug = "news",
        };
        var cat2 = new NewsCategory
        {
            Id = Guid.NewGuid(),
            Name = "Мероприятия",
            Slug = "events",
        };
        _db.NewsCategories.AddRange(cat1, cat2);

        var news1 = NewsFixture.CreateFaker().Generate();
        news1.CategoryId = cat1.Id;
        news1.CreatedById = _adminId;
        var news2 = NewsFixture.CreateFaker().Generate();
        news2.CategoryId = cat2.Id;
        news2.CreatedById = _adminId;
        _db.News.AddRange(news1, news2);
        await _db.SaveChangesAsync();

        var result = await _sut.GetAllAsync(1, 10, cat1.Id, null, default);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Items.Should().AllSatisfy(n => n.CategoryId.Should().Be(cat1.Id));
    }

    [Fact]
    public async Task GetAllAsync_SearchesByTitle()
    {
        var matching = NewsFixture.CreateFaker().Generate();
        matching.Title = "День открытых дверей";
        matching.CreatedById = _adminId;
        var notMatching = NewsFixture.CreateFaker().Generate();
        notMatching.Title = "Расписание экзаменов";
        notMatching.CreatedById = _adminId;
        _db.News.AddRange(matching, notMatching);
        await _db.SaveChangesAsync();

        var result = await _sut.GetAllAsync(1, 10, null, "дверей", default);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Items.Should().HaveCount(1);
        result.Data.Items[0].Title.Should().Be("День открытых дверей");
    }

    [Fact]
    public async Task CreateAsync_Returns404_WhenCategoryNotFound()
    {
        var request = new CreateNewsRequest
        {
            Title = "Новость",
            Content = "Контент",
            CategoryId = Guid.NewGuid(),
        };

        var result = await _sut.CreateAsync(request, _adminId, default);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
    }
}
