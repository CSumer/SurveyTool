namespace SurveyTool.Api.Contracts.Responses;

public record ResponseDetailsDto(
    int Id,
    int SurveyId,
    DateTime CreatedAt,
    int Score,
    IEnumerable<ResponseItemDetailsDto> Items
);

public record ResponseItemDetailsDto(
    int QuestionId,
    IEnumerable<int>? SelectedOptionIds,
    string? FreeText
);