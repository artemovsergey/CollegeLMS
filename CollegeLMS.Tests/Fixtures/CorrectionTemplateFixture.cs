namespace CollegeLMS.Tests.Fixtures;

/// <summary>
/// Шаблон «Корректировка.xlsx», который <c>ScheduleCorrectionService</c> ищет
/// по пути <c>../import/schedule</c> относительно рабочего каталога. Папка
/// общая для всех тестов экспорта, поэтому её надо подготовить заранее.
///
/// xUnit гоняет классы параллельно, и класс, не готовивший шаблон сам, мог
/// стартовать раньше остальных и получать отказ «Шаблон корректировки
/// отсутствует на сервере» — тесты падали в зависимости от нагрузки раннера.
/// Блокировка делает подготовку идемпотентной: копирует первый, остальные
/// видят готовый файл.
/// </summary>
public static class CorrectionTemplateFixture
{
    private static readonly object Gate = new();

    public static void Ensure()
    {
        lock (Gate)
        {
            EnsureCore();
        }
    }

    private static void EnsureCore()
    {
        var relativeDir = Path.GetFullPath(
            Path.Combine(Directory.GetCurrentDirectory(), "..", "import", "schedule")
        );
        var target = Path.Combine(relativeDir, "Корректировка.xlsx");
        if (File.Exists(target))
            return;

        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, "import", "schedule", "Корректировка.xlsx");
            if (File.Exists(candidate))
            {
                Directory.CreateDirectory(relativeDir);
                File.Copy(candidate, target, overwrite: true);
                return;
            }
            dir = dir.Parent;
        }

        throw new FileNotFoundException(
            "Шаблон «Корректировка.xlsx» не найден для теста экспорта."
        );
    }
}