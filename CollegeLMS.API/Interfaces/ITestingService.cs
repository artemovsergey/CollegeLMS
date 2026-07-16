using CollegeLMS.API.Dtos;
using CollegeLMS.API.Response;

namespace CollegeLMS.API.Interfaces;

public interface ITestingService
{
    Task<Result<List<TestResponse>>> GetAllAsync(Guid? courseId, Guid currentUserId, string currentUserRole, CancellationToken ct = default);
    Task<Result<TestResponse>> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Result<TestResponse>> CreateAsync(CreateTestRequest request, Guid currentUserId, string currentUserRole, CancellationToken ct = default);
    Task<Result<TestResponse>> UpdateAsync(Guid id, UpdateTestRequest request, Guid currentUserId, string currentUserRole, CancellationToken ct = default);
    Task<Result> DeleteAsync(Guid id, Guid currentUserId, string currentUserRole, CancellationToken ct = default);
    Task<Result<List<QuestionResponse>>> GetQuestionsAsync(Guid testId, Guid currentUserId, string currentUserRole, CancellationToken ct = default);
    Task<Result<QuestionResponse>> CreateQuestionAsync(Guid testId, CreateQuestionRequest request, Guid currentUserId, string currentUserRole, CancellationToken ct = default);
    Task<Result<QuestionResponse>> UpdateQuestionAsync(Guid testId, Guid questionId, UpdateQuestionRequest request, Guid currentUserId, string currentUserRole, CancellationToken ct = default);
    Task<Result> DeleteQuestionAsync(Guid testId, Guid questionId, Guid currentUserId, string currentUserRole, CancellationToken ct = default);
    Task<Result<TestAssignmentResponse>> AssignTestAsync(Guid testId, AssignTestRequest request, Guid currentUserId, string currentUserRole, CancellationToken ct = default);
    Task<Result<List<TestAssignmentResponse>>> GetAssignmentsAsync(Guid testId, Guid currentUserId, string currentUserRole, CancellationToken ct = default);
    Task<Result> RemoveAssignmentAsync(Guid testId, Guid assignmentId, Guid currentUserId, string currentUserRole, CancellationToken ct = default);
    Task<Result<TestResponse>> UpdateSettingsAsync(Guid testId, TestSettingsRequest request, Guid currentUserId, string currentUserRole, CancellationToken ct = default);
    Task<Result<StartTestResponse>> StartTestAsync(Guid testId, Guid currentUserId, CancellationToken ct = default);
    Task<Result<AttemptResponse>> SubmitAnswersAsync(Guid testId, Guid attemptId, SubmitAnswersRequest request, Guid currentUserId, CancellationToken ct = default);
    Task<Result<List<AttemptResponse>>> GetMyAttemptsAsync(Guid testId, Guid currentUserId, CancellationToken ct = default);
    Task<Result<TestResultResponse>> GetMyResultAsync(Guid testId, Guid currentUserId, CancellationToken ct = default);
    Task<Result<List<AttemptResponse>>> GetAllMyResultsAsync(Guid currentUserId, CancellationToken ct = default);
    Task<Result<TestStatsResponse>> GetStatsAsync(Guid testId, Guid currentUserId, string currentUserRole, CancellationToken ct = default);
}
