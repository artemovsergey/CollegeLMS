namespace CollegeLMS.API.Dtos;

/// <summary>Профиль звонков: дни недели, диапазоны дат, пары и большая перемена.</summary>
public class BellProfileResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
    public List<int> DaysOfWeek { get; set; } = [];
    public List<BellSlotResponse> Slots { get; set; } = [];
    public BigBreakResponse? BigBreak { get; set; }
    public List<BellProfileDateResponse> Dates { get; set; } = [];
}

/// <summary>Диапазон дат, на который действует профиль звонков.</summary>
public class BellProfileDateResponse
{
    public Guid Id { get; set; }
    public DateTime DateFrom { get; set; }
    public DateTime DateTo { get; set; }
}

/// <summary>Данные для создания или изменения профиля звонков.</summary>
public class BellProfileRequest
{
    public string Name { get; set; } = string.Empty;
    public List<int> DaysOfWeek { get; set; } = [];
    public List<BellSlotRequest> Slots { get; set; } = [];
    public BigBreakRequest? BigBreak { get; set; }
    public List<BellProfileDateRequest> Dates { get; set; } = [];
}

/// <summary>Диапазон дат профиля звонков в запросе.</summary>
public class BellProfileDateRequest
{
    public DateTime DateFrom { get; set; }
    public DateTime DateTo { get; set; }
}
