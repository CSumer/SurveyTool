namespace SurveyTool.Api.Contracts.Responses;

public record ResponseSummaryDto(
    int Id,
    int SurveyId,
    DateTime CreatedAt,
    int Score
);