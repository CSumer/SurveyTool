namespace SurveyTool.Api.Contracts.Questions;

public record CreateQuestionDto(
    string Text,
    QuestionType Type,
    int? ParentQuestionId,
    IEnumerable<int>? ShowWhenAnyOptionSelected);