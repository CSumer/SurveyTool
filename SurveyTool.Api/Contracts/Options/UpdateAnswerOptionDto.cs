using System.ComponentModel.DataAnnotations;

namespace SurveyTool.Api.Contracts.Options;

public record UpdateAnswerOptionDto(
    [property: Required, MinLength(1), MaxLength(200)] string Text,
    int Weight
);