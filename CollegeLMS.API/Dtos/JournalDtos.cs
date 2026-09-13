namespace CollegeLMS.API.Dtos;

public class JournalResponse
{
    public Guid TeacherId { get; set; }
    public string TeacherName { get; set; } = string.Empty;
    public List<JournalSubjectGroup> Subjects { get; set; } = new();
    public int TotalPairCount { get; set; }
}

public class JournalSubjectGroup
{
    public string Subject { get; set; } = string.Empty;
    public List<JournalEntryItem> Items { get; set; } = new();
    public int PairCount { get; set; }
}

public class JournalEntryItem
{
    public int Week { get; set; }
    public DateTime Date { get; set; }
    public List<int> NumberPairs { get; set; } = new();
}