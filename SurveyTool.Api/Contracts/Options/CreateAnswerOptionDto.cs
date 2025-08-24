using System.ComponentModel.DataAnnotations;

namespace SurveyTool.Api.Contracts.Options;

public record CreateAnswerOptionDto(
    [property: Required]
    [property: MinLength(1)]
    [property: MaxLength(200)]
    string Text,
    int Weight
);
