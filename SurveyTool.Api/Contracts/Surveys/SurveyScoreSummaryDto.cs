namespace SurveyTool.Api.Contracts.Surveys;

public record SurveyScoreSummaryDto(
    int SurveyId,
    int TotalScore,
    int ResponseCount,
    double AverageScore
);
