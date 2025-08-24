using SurveyTool.Api.Contracts.Questions;

namespace SurveyTool.Api.Contracts.Surveys;

public record SurveyDto(int Id, string Title, string? Description, IEnumerable<QuestionDto> Questions);