using CollegeLMS.Shared;
using FluentAssertions;
using Xunit;

namespace CollegeLMS.MaxBot.Tests;

/// <summary>
/// Словарь терминов — общий для бота и PNG-корректировки.
///
/// Тест фиксирует его намеренно: это копия веб-словаря
/// <c>CollegeLMS.Next/lib/change-tags.ts</c>, и раньше копии разъезжались
/// молча — «Добавлено», «добавлена», «Изменено» на трёх поверхностях, знаки
/// <c>→</c> и <c>=&gt;</c> вперемешку. Без теста следующая правка словаря
/// снова разойдётся незаметно.
/// </summary>
public class ChangeVocabularyTests
{
    [Theory]
    [InlineData("Add", "Добавлено")]
    [InlineData("Remove", "Снято")]
    [InlineData("Replace", "Добавлено")]
    [InlineData("Move", "Добавлено")]
    public void Label_MatchesReaderVocabulary(string changeType, string expected)
    {
        ChangeVocabulary.Label(changeType).Should().Be(expected);
    }

    [Fact]
    public void Label_UnknownType_IsNotEmpty()
    {
        // Неизвестный исход не должен молчать: в чате это единственное
        // объяснение, что произошло.
        ChangeVocabulary.Label("Whatever").Should().Be("Изменено");
    }

    [Fact]
    public void ChangeArrow_IsAscii()
    {
        // В чате шрифт чужой, и `→` отображается в нём по-разному. В PNG
        // тот же знак печатается шрифтом рендера. ASCII-строка предсказуема
        // в обоих случаях.
        ChangeVocabulary.ChangeArrow.Should().Be("=>");
        ChangeVocabulary.ChangeArrow.Should().NotContain("→");
    }
}
