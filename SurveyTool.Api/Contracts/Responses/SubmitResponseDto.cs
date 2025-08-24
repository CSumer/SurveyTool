using SurveyTool.Core;

namespace SurveyTool.Api.Contracts.Responses;
public record SubmitResponseDto(IEnumerable<ResponseItemDto> Items);