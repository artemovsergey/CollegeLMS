using CollegeLMS.MaxBot.Clients;
using CollegeLMS.MaxBot.Services;

namespace CollegeLMS.MaxBot.Tests;

public class DispatcherCorrectionWizardTests
{
    private static DispatcherWizardState State(string changeType) =>
        new()
        {
            Step = DispatcherWizardStep.Note,
            ChangeType = changeType,
            Day = 2, // Вторник
            Week = 3,
            GroupId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            GroupName = "ИС-21",
            RemovedNumberPair = 2,
            RemovedSubject = "История",
            RemovedTeacherId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
            RemovedTeacherName = "Петров П.П.",
            NewPair = 4,
            Subject = "Информатика",
            TeacherId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
            TeacherName = "Иванов И.И.",
        };

    [Fact]
    public void BuildEntry_Add_FillsNewLessonFields()
    {
        var state = State("Add");
        state.RemovedNumberPair = null;
        state.RemovedSubject = null;
        state.RemovedTeacherId = null;
        state.RemovedTeacherName = null;

        var entry = DispatcherCorrectionWizard.BuildEntry(state);

        entry.ChangeType.Should().Be("Add");
        entry.DayOfWeek.Should().Be(2);
        entry.Week.Should().Be(3);
        entry.GroupId.Should().Be(state.GroupId);
        entry.GroupName.Should().Be("ИС-21");
        entry.NumberPair.Should().Be(4);
        entry.Subject.Should().Be("Информатика");
        entry.TeacherId.Should().Be(state.TeacherId);
        entry.TeacherName.Should().Be("Иванов И.И.");
        entry.RemovedNumberPair.Should().BeNull();
        entry.RemovedSubject.Should().BeNull();
        entry.Row.Should().Be(1);
    }

    [Fact]
    public void BuildEntry_Remove_HasNoNewSubject()
    {
        var state = State("Remove");
        state.Subject = null;
        state.TeacherId = null;
        state.TeacherName = null;
        state.NewPair = null;

        var entry = DispatcherCorrectionWizard.BuildEntry(state);

        entry.ChangeType.Should().Be("Remove");
        entry.NumberPair.Should().Be(2);
        entry.Subject.Should().BeNull();
        entry.TeacherId.Should().BeNull();
        entry.RemovedSubject.Should().Be("История");
        entry.RemovedNumberPair.Should().Be(2);
        entry.RemovedTeacherName.Should().Be("Петров П.П.");
    }

    [Fact]
    public void BuildEntry_Replace_NewSubjectAtRemovedSlot()
    {
        var entry = DispatcherCorrectionWizard.BuildEntry(State("Replace"));

        entry.ChangeType.Should().Be("Replace");
        entry.NumberPair.Should().Be(2); // слот снимаемой пары
        entry.Subject.Should().Be("Информатика");
        entry.RemovedSubject.Should().Be("История");
        entry.RemovedNumberPair.Should().Be(2);
    }

    [Fact]
    public void BuildEntry_Move_NewSlotAndRemovedSlot()
    {
        var entry = DispatcherCorrectionWizard.BuildEntry(State("Move"));

        entry.ChangeType.Should().Be("Move");
        entry.NumberPair.Should().Be(4); // новая пара
        entry.RemovedNumberPair.Should().Be(2);
        entry.RemovedSubject.Should().Be("История");
    }

    [Fact]
    public void BuildEntry_NoteDash_BecomesNull()
    {
        var state = State("Add");
        state.Note = "—";

        var entry = DispatcherCorrectionWizard.BuildEntry(state);

        entry.Note.Should().BeNull();
    }

    [Fact]
    public void BuildEntry_NoteText_IsKept()
    {
        var state = State("Add");
        state.Note = "кабинет заменить";

        var entry = DispatcherCorrectionWizard.BuildEntry(state);

        entry.Note.Should().Be("кабинет заменить");
    }

    [Fact]
    public void BuildWeekGrid_RowsOfFour()
    {
        var buttons = DispatcherCorrectionWizard.BuildWeekGrid(1, 16);

        buttons.Should().HaveCount(4);
        buttons[0].Should().HaveCount(4);
        buttons.Sum(r => r.Count).Should().Be(16);
        buttons[0][0].Payload.Should().Be("wiz:week:1");
        buttons[3][3].Payload.Should().Be("wiz:week:16");
    }

    [Fact]
    public void BuildWeekGrid_CurrentWeekMarked()
    {
        var buttons = DispatcherCorrectionWizard.BuildWeekGrid(3, 16);

        buttons[0][2].Text.Should().Be("• 3");
        buttons[0][0].Text.Should().Be("1");
    }

    [Fact]
    public void BuildPreview_ContainsTypeGroupAndPair()
    {
        var text = DispatcherCorrectionWizard.BuildPreview(State("Replace"));

        text.Should().Contain("замена");
        text.Should().Contain("ИС-21");
        text.Should().Contain("Пара: 2");
        text.Should().Contain("Снимается: История");
    }
}
