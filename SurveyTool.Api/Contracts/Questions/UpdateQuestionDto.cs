using System.ComponentModel.DataAnnotations;

namespace SurveyTool.Api.Contracts.Questions;

public record UpdateQuestionDto(
    [property: Required, MinLength(3), MaxLength(500)] string Text,
    [property: Required] QuestionType Type,
    int? ParentQuestionId,
    IEnumerable<int>? ShowWhenAnyOptionSelected
);