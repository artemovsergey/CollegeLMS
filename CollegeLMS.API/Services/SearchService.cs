using CollegeLMS.API.Data;
using CollegeLMS.API.Dtos;
using CollegeLMS.API.Interfaces;
using CollegeLMS.API.Response;
using Microsoft.EntityFrameworkCore;

namespace CollegeLMS.API.Services;

public class SearchService(AppDbContext db) : ISearchService
{
    private static readonly List<StaticPageEntry> StaticPages =
    [
        new("Сведения об ОО", "/about", "about"),
        new("Основные сведения", "/about/common", "about"),
        new("Структура и органы управления", "/about/struct", "about"),
        new("Документы", "/about/document", "about"),
        new("Образование", "/about/education", "about"),
        new("Образовательные стандарты", "/about/edustandarts", "about"),
        new("Руководство", "/about/rucovodstvo", "about"),
        new("Педагогический состав", "/about/teachingstaff", "about"),
        new("Материально-техническое обеспечение", "/about/objects", "about"),
        new("Организация питания", "/about/meals", "about"),
        new("Стипендии и меры поддержки", "/about/grants", "about"),
        new("Платные образовательные услуги", "/about/paid_edu", "about"),
        new("Финансово-хозяйственная деятельность", "/about/budget", "about"),
        new("Вакантные места", "/about/vacant", "about"),
        new("Международное сотрудничество", "/about/inter", "about"),
        new("Свидетельство об аккредитации", "/about/svidetelstvo-ob-akkreditatsii", "about"),
        new("Колледж", "/college", "college"),
        new("О колледже", "/college/o-kolledzhe", "college"),
        new("История создания", "/college/istoriya-sozdaniya-kolledzha", "college"),
        new("Воспитательная работа", "/college/vospitatelnaya-rabota", "college"),
        new("Наставничество", "/college/nastavnichestvo", "college"),
        new("Общежитие", "/college/obshhezhitie", "college"),
        new("Профсоюзная организация", "/college/pervichnaya-profsoyuznaya-organizatsiya", "college"),
        new("Независимая оценка качества", "/college/nezavisimaya-otsenka-kachestva-uslovij-osushhestvleniya-obrazovatelnoj-deyatelnosti", "college"),
        new("План работы", "/college/plan-raboty-gbpou-sks-na-tekushhij-uchebnyj-god", "college"),
        new("Контакты", "/college/polnaya-kontaktnaya-informatsiya", "college"),
        new("Образование", "/education", "education"),
        new("Специальности", "/education/perechen-spetsialnostey", "education"),
        new("Курсы", "/education/kursyi", "education"),
        new("Целевое обучение", "/education/tselevoe-obuchenie", "education"),
        new("Оплата услуг", "/education/oplata-uslug", "education"),
        new("Образовательный кредит", "/education/obrazovatelnyj-kredit", "education"),
        new("Абитуриенту", "/admissions", "admissions"),
        new("Приемная комиссия", "/admissions/priemnaya-komissiya", "admissions"),
        new("Специальности", "/admissions/perechen-spetsialnostey", "admissions"),
        new("Дни открытых дверей", "/admissions/den-otkrytyh-dverej-2026", "admissions"),
        new("Приказы о зачислении", "/admissions/prikazy-na-zachislenie-2025", "admissions"),
        new("Документы для приема", "/admissions/kakie-dokumentyi-neobhodimo-prinesti", "admissions"),
        new("Общежитие", "/admissions/obshhezhitie", "admissions"),
        new("Количество заявлений", "/admissions/kolichestvo-podannyih-zayavleniy", "admissions"),
        new("Задать вопрос", "/admissions/8008-2", "admissions"),
        new("Студенту", "/student-life", "student-life"),
        new("Расписание занятий", "/student-life/raspisanie-zanyatij-po-ochnoj-forme-obucheniya", "student-life"),
        new("Расписание экзаменов", "/student-life/raspisanie-ekzamenov", "student-life"),
        new("Государственная итоговая аттестация", "/student-life/raspisanie-gosudarstvennoj-itogovoj-attestatsii", "student-life"),
        new("Задолженности", "/student-life/raspisanie-likvidatsii-akademicheskih-zadolzhennostej", "student-life"),
        new("Библиотека", "/student-life/biblioteka", "student-life"),
        new("Дистанционное обучение", "/student-life/distancionnoeobuch", "student-life"),
        new("Трудоустройство", "/student-life/trudoustroystvo", "student-life"),
    ];

    public async Task<Result<PagedResponse<SearchResponse>>> SearchAsync(
        string query,
        int page,
        int pageSize,
        CancellationToken ct
    )
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        if (string.IsNullOrWhiteSpace(query))
        {
            return Result<PagedResponse<SearchResponse>>.Ok(
                new PagedResponse<SearchResponse>([], 0, page, pageSize)
            );
        }

        var term = query.ToLower();

        var newsResults = await db
            .News.AsNoTracking()
            .Where(n =>
                !n.IsDeleted
                && n.IsPublished
                && (n.Title.ToLower().Contains(term) || n.Content.ToLower().Contains(term))
            )
            .Select(n => new SearchResponse
            {
                Title = n.Title,
                Type = "news",
                Url = $"/news/{n.Id}",
                Snippet = n.Content.Substring(0, Math.Min(n.Content.Length, 200)) + (n.Content.Length > 200 ? "..." : ""),
                Score =
                    n.Title.ToLower() == term ? 100 :
                    n.Title.ToLower().Contains(term) ? 50 :
                    10,
            })
            .ToListAsync(ct);

        var pageResults = StaticPages
            .Select(p => new
            {
                Entry = p,
                LowerTitle = p.Title.ToLower(),
            })
            .Where(x => x.LowerTitle.Contains(term))
            .Select(x => new SearchResponse
            {
                Title = x.Entry.Title,
                Type = "page",
                Url = x.Entry.Url,
                Snippet = $"Раздел: {x.Entry.Section}",
                Score = x.LowerTitle == term ? 100 : 50,
            })
            .ToList();

        var allResults = newsResults
            .Concat(pageResults)
            .OrderByDescending(r => r.Score)
            .ThenBy(r => r.Title)
            .ToList();

        var totalCount = allResults.Count;
        var items = allResults
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return Result<PagedResponse<SearchResponse>>.Ok(
            new PagedResponse<SearchResponse>(items, totalCount, page, pageSize)
        );
    }

    private record StaticPageEntry(string Title, string Url, string Section);
}
