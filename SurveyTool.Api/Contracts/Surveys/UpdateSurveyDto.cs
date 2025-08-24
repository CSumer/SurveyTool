using System.ComponentModel.DataAnnotations;

namespace SurveyTool.Api.Contracts.Surveys;

public record UpdateSurveyDto(
    [property: Required, MinLength(3), MaxLength(200)]
    string Title,
    string? Description
);
