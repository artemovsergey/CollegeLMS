using CollegeLMS.API.Services;
using Xunit;

namespace CollegeLMS.Tests.Unit.Services;

/// <summary>
/// Написание предмета. Диспетчер пишет его руками, поэтому одно и то же
/// название приходит десятком способов: «Ин.язык(1и2)», «ОА и П»,
/// «Электротех.и Э.». Нормализация обязана приводить всё к тому написанию,
/// которое лежит в устойчивом расписании, — иначе перенос «вм.X» не находит
/// занятие, а в выгруженном файле предмет называется иначе, чем в расписании.
/// </summary>
public class SubjectNormalizeTests
{
    /// <summary>
    /// Названия из устойчивого расписания. Нормализация не должна их менять:
    /// иначе импорт расписания начнёт переименовывать предметы.
    /// </summary>
    [Theory]
    [InlineData("Ин.язык")]
    [InlineData("Обществоз.")]
    [InlineData("ЭлектрТех.")]
    [InlineData("ОсновыЭлектр.")]
    [InlineData("Теор.ЭлектрТех.")]
    [InlineData("Сил.ЭлектрТех.")]
    [InlineData("Систем.ЭлектрТех.")]
    [InlineData("ЭлектрБезопасность")]
    [InlineData("Физкультура")]
    [InlineData("ИсторияРоссии")]
    [InlineData("Математика")]
    [InlineData("ОхранаТруда")]
    [InlineData("ЭкономОтр.")]
    [InlineData("Общ.энерг.")]
    [InlineData("Менеджм.вПД")]
    [InlineData("Основы web-прогр.")]
    [InlineData("ОБПиЗР")]
    [InlineData("ОПиФГ")]
    [InlineData("ОСиС")]
    [InlineData("ОЭиВТ")]
    [InlineData("ОАиП")]
    [InlineData("ССиТДВ")]
    [InlineData("ТВиМС")]
    [InlineData("ПКПвПД")]
    [InlineData("ИиВТ")]
    [InlineData("МДК.01.02")]
    [InlineData("Материал.")]
    [InlineData("Рус.язык")]
    public void Название_из_расписания_не_меняется(string subject)
    {
        Assert.Equal(subject, ScheduleImportService.NormalizeSubject(subject));
    }

    /// <summary>Написание, которое встречается в файлах корректировок.</summary>
    [Theory]
    [InlineData("Ин.язык(1и2)", "Ин.язык")]
    [InlineData("Ин.язык (1и2)", "Ин.язык")]
    [InlineData("Ин.язык(1 и 2)", "Ин.язык")]
    [InlineData("Обществ.", "Обществоз.")]
    [InlineData("История России", "ИсторияРоссии")]
    [InlineData("Ист.Р.", "ИсторияРоссии")]
    [InlineData("Общ.энер.", "Общ.энерг.")]
    [InlineData("Основы электр.", "ОсновыЭлектр.")]
    [InlineData("ОсновыЭлектрТех.", "ОсновыЭлектр.")]
    [InlineData("Теор.электр.", "Теор.ЭлектрТех.")]
    [InlineData("Сил.электр.", "Сил.ЭлектрТех.")]
    [InlineData("Систем.электр.", "Систем.ЭлектрТех.")]
    [InlineData("Электротех.", "ЭлектрТех.")]
    [InlineData("Электротех.и Э.", "ЭлектрТех.")]
    [InlineData("Электр.и Э.", "ЭлектрТех.")]
    [InlineData("Электр.тех.", "ЭлектрТех.")]
    [InlineData("Эл.тех.", "ЭлектрТех.")]
    [InlineData("Электробез.", "ЭлектрБезопасность")]
    [InlineData("Физ.культура", "Физкультура")]
    [InlineData("Физ.кул.", "Физкультура")]
    [InlineData("Физ.культ", "Физкультура")]
    [InlineData("Мен.в ПД", "Менеджм.вПД")]
    [InlineData("Менеджм.в ПД", "Менеджм.вПД")]
    [InlineData("ТЭС", "ТЭЦ")]
    [InlineData("Охрана труда", "ОхранаТруда")]
    [InlineData("Эконом.отр.", "ЭкономОтр.")]
    [InlineData("Осн.web.пр.", "Основы web-прогр.")]
    [InlineData("ТЭС", "ТЭЦ")]
    [InlineData("МДК.01.02.", "МДК.01.02")]
    [InlineData("ОБП и ЗР", "ОБПиЗР")]
    [InlineData("ОП и ФГ", "ОПиФГ")]
    [InlineData("ОС и С", "ОСиС")]
    [InlineData("ОЭ и ВТ", "ОЭиВТ")]
    [InlineData("ОА и П", "ОАиП")]
    [InlineData("СС и ТДВ", "ССиТДВ")]
    [InlineData("ТВ и МС", "ТВиМС")]
    [InlineData("ПКП в ПД", "ПКПвПД")]
    [InlineData("И и ВТ", "ИиВТ")]
    public void Написание_диспетчера_приводится_к_названию_расписания(
        string written,
        string canonical
    )
    {
        Assert.Equal(canonical, ScheduleImportService.NormalizeSubject(written));
    }

    /// <summary>
    /// Предметы, у которых уточнение отличает один от другого. Свести их в один
    /// нельзя: «Теор.ЭлектрТех.» и «ЭлектрТех.» — разные дисциплины.
    /// </summary>
    [Theory]
    [InlineData("ЭлектрТех.")]
    [InlineData("Теор.ЭлектрТех.")]
    [InlineData("Сил.ЭлектрТех.")]
    [InlineData("Систем.ЭлектрТех.")]
    [InlineData("ОсновыЭлектр.")]
    [InlineData("ЭлектрБезопасность")]
    public void Уточнённые_предметы_не_сливаются(string subject)
    {
        var others = new[]
        {
            "ЭлектрТех.",
            "Теор.ЭлектрТех.",
            "Сил.ЭлектрТех.",
            "Систем.ЭлектрТех.",
            "ОсновыЭлектр.",
            "ЭлектрБезопасность",
        }
            .Where(other => other != subject)
            .Select(other => ScheduleImportService.SubjectLookupKey(other));

        Assert.DoesNotContain(ScheduleImportService.SubjectLookupKey(subject), others);
    }

    /// <summary>Нормализация должна быть идемпотентной: иначе она ломает сама себя.</summary>
    [Theory]
    [InlineData("Ин.язык(1и2)")]
    [InlineData("Общ.энер.")]
    [InlineData("Электробез.")]
    [InlineData("Основы электр.")]
    [InlineData("Электротех.и Э.")]
    [InlineData("МДК.01.02.")]
    [InlineData("ОА и П")]
    public void Повторная_нормализация_ничего_не_меняет(string written)
    {
        var once = ScheduleImportService.NormalizeSubject(written);
        Assert.Equal(once, ScheduleImportService.NormalizeSubject(once));
    }

    /// <summary>
    /// Ключ сравнения — то, по чему предмет ищется в расписании. Разные
    /// написания одного предмета должны давать один ключ: от этого зависит,
    /// найдёт ли перенос «вм.X» занятие в паре «откуда».
    /// </summary>
    [Theory]
    [InlineData("Ин.язык(1и2)", "Ин.язык")]
    [InlineData("Обществ.", "Обществоз.")]
    [InlineData("Основы электр.", "ОсновыЭлектр.")]
    [InlineData("ОА и П", "ОАиП")]
    [InlineData("Электротех.и Э.", "ЭлектрТех.")]
    [InlineData("МДК.04.02.", "МДК.04.02")]
    public void Один_предмет_даёт_один_ключ_независимо_от_написания(
        string written,
        string canonical
    )
    {
        Assert.Equal(
            ScheduleImportService.SubjectLookupKey(canonical),
            ScheduleImportService.SubjectLookupKey(written)
        );
    }

    /// <summary>Пустой предмет остаётся пустым, а не превращается в строку.</summary>
    [Fact]
    public void Пустой_предмет_остаётся_пустым()
    {
        Assert.Equal(string.Empty, ScheduleImportService.NormalizeSubject(string.Empty));
        Assert.Equal(string.Empty, ScheduleImportService.NormalizeSubject("   "));
    }
}
