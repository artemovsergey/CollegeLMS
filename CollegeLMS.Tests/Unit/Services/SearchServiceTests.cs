using CollegeLMS.API.Entities;
using CollegeLMS.API.Services;
using CollegeLMS.Tests.Fixtures;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace CollegeLMS.Tests.Unit.Services;

public class SearchServiceTests : IDisposable
{
    private readonly API.Data.AppDbContext _db;
    private readonly SearchService _sut;
    private readonly Guid _adminId;

    public SearchServiceTests()
    {
        _db = TestDbContextFactory.Create();
        _sut = new SearchService(_db);
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
                Role = API.Entities.Enums.UserRole.Admin,
                IsActive = true,
            }
        );
        _db.SaveChanges();
        return id;
    }

    private News CreatePublishedNews(string title, string content = "Содержание")
    {
        return new News
        {
            Id = Guid.NewGuid(),
            Title = title,
            Content = content,
            IsPublished = true,
            IsDeleted = false,
            CreatedById = _adminId,
            PublishedAt = DateTime.UtcNow,
        };
    }

    [Fact]
    public async Task SearchAsync_EmptyQuery_ReturnsEmptyResults()
    {
        var result = await _sut.SearchAsync("", 1, 20, default);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Items.Should().BeEmpty();
        result.Data.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task SearchAsync_WhitespaceQuery_ReturnsEmptyResults()
    {
        var result = await _sut.SearchAsync("   ", 1, 20, default);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task SearchAsync_FindsNewsByTitle()
    {
        var matching = CreatePublishedNews("День открытых дверей");
        var notMatching = CreatePublishedNews("Расписание экзаменов");
        _db.News.AddRange(matching, notMatching);
        await _db.SaveChangesAsync();

        var result = await _sut.SearchAsync("дверей", 1, 20, default);

        result.IsSuccess.Should().BeTrue();
        result
            .Data!.Items.Should()
            .Contain(i => i.Type == "news" && i.Title == "День открытых дверей");
    }

    [Fact]
    public async Task SearchAsync_FindsNewsByContent()
    {
        var news = CreatePublishedNews("Новость", "Обновлённое содержание о студентах");
        _db.News.Add(news);
        await _db.SaveChangesAsync();

        var result = await _sut.SearchAsync("студентах", 1, 20, default);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Items.Should().HaveCount(1);
        result.Data.Items[0].Type.Should().Be("news");
    }

    [Fact]
    public async Task SearchAsync_ExcludesDeletedNews()
    {
        var news = CreatePublishedNews("Удалённая новость");
        news.IsDeleted = true;
        _db.News.Add(news);
        await _db.SaveChangesAsync();

        var result = await _sut.SearchAsync("Удалённая", 1, 20, default);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task SearchAsync_ExcludesUnpublishedNews()
    {
        var news = CreatePublishedNews("Черновик новости");
        news.IsPublished = false;
        _db.News.Add(news);
        await _db.SaveChangesAsync();

        var result = await _sut.SearchAsync("Черновик", 1, 20, default);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task SearchAsync_FindsStaticPages()
    {
        var result = await _sut.SearchAsync("Колледж", 1, 20, default);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Items.Should().Contain(i => i.Type == "page");
        result.Data.Items.Should().Contain(i => i.Title == "Колледж");
    }

    [Fact]
    public async Task SearchAsync_CombinesNewsAndPages()
    {
        var news = CreatePublishedNews("Новости колледжа");
        _db.News.Add(news);
        await _db.SaveChangesAsync();

        var result = await _sut.SearchAsync("колледж", 1, 20, default);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Items.Should().Contain(i => i.Type == "news");
        result.Data.Items.Should().Contain(i => i.Type == "page");
    }

    [Fact]
    public async Task SearchAsync_RespectsPagination()
    {
        for (var i = 0; i < 5; i++)
        {
            _db.News.Add(CreatePublishedNews($"Новость колледжа {i}"));
        }
        await _db.SaveChangesAsync();

        var result = await _sut.SearchAsync("колледж", 1, 2, default);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Items.Should().HaveCount(2);
        result.Data.TotalCount.Should().BeGreaterThan(2);
    }

    [Fact]
    public async Task SearchAsync_NoMatch_ReturnsEmpty()
    {
        var result = await _sut.SearchAsync("несуществующийзапросxyz", 1, 20, default);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Items.Should().BeEmpty();
        result.Data.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task SearchAsync_SnippetTruncatesLongContent()
    {
        var longContent = new string('а', 300);
        var news = CreatePublishedNews("Длинная новость", longContent);
        _db.News.Add(news);
        await _db.SaveChangesAsync();

        var result = await _sut.SearchAsync("Длинная", 1, 20, default);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Items.Should().HaveCount(1);
        result.Data.Items[0].Snippet.Length.Should().Be(203);
        result.Data.Items[0].Snippet.Should().EndWith("...");
    }
}
