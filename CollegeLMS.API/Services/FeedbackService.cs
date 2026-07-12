using CollegeLMS.API.Data;
using CollegeLMS.API.Dtos;
using CollegeLMS.API.Interfaces;
using CollegeLMS.API.Mappers;
using CollegeLMS.API.Response;
using Microsoft.EntityFrameworkCore;

namespace CollegeLMS.API.Services;

public class FeedbackService(AppDbContext db) : IFeedbackService
{
    public async Task<Result<List<FeedbackListItemDto>>> GetAllAsync(CancellationToken ct)
    {
        var items = await db
            .Set<Entities.Feedback>()
            .AsNoTracking()
            .OrderByDescending(f => f.CreatedAt)
            .Select(f => f.ToListItemDto())
            .ToListAsync(ct);

        return Result<List<FeedbackListItemDto>>.Ok(items);
    }


    public async Task<Result<FeedbackResponse>> CreateAsync(
        FeedbackRequest request,
        CancellationToken ct
    )
    {
        var recent = await db.Set<Entities.Feedback>()
            .Where(f => f.Email == request.Email)
            .OrderByDescending(f => f.CreatedAt)
            .FirstOrDefaultAsync(ct);

        if (recent is not null && (DateTime.UtcNow - recent.CreatedAt).TotalMinutes < 5)
            return Result<FeedbackResponse>.Fail(
                "Вы уже отправляли сообщение. Попробуйте позже.",
                429
            );

        var feedback = request.ToEntity();
        db.Set<Entities.Feedback>().Add(feedback);
        await db.SaveChangesAsync(ct);

        return Result<FeedbackResponse>.Ok(
            new FeedbackResponse { Message = "Сообщение отправлено" }
        );
    }
}
