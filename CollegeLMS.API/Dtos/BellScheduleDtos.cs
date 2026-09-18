namespace CollegeLMS.API.Dtos;

public class BellScheduleResponse
{
    public List<BellSlotResponse> Slots { get; set; } = [];
    public BigBreakResponse? BigBreak { get; set; }
}

public class BellSlotResponse
{
    public Guid Id { get; set; }
    public int NumberPair { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
}

public class BigBreakResponse
{
    public Guid Id { get; set; }
    public int AfterPair { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
}

public class UpdateBellScheduleRequest
{
    public List<BellSlotRequest> Slots { get; set; } = [];
    public BigBreakRequest? BigBreak { get; set; }
}

public class BellSlotRequest
{
    public int NumberPair { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
}

public class BigBreakRequest
{
    public int AfterPair { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
}
