namespace SurveyTool.Api.Contracts.Responses;
public record ResponseItemDto(int QuestionId, IEnumerable<int>? SelectedOptionIds, string? FreeText);