using SurveyTool.Api.Contracts.Responses;
using SurveyTool.Core.Domain;

namespace SurveyTool.Api.Mapping
{
    public static class ResponseReadMappers
    {
        public static ResponseDetailsDto ToDetailsDto(this SurveyResponse r) =>
            new(
                r.Id,
                r.SurveyId,
                r.CreatedAt,
                r.Score,
                r.Items.Select(i => new ResponseItemDetailsDto(
                    i.QuestionId,
                    ParseIds(i.SelectedOptionIdsCsv),
                    i.FreeText
                ))
            );

        public static ResponseSummaryDto ToSummaryDto(this SurveyResponse r) =>
            new(r.Id, r.SurveyId, r.CreatedAt, r.Score);

        private static IEnumerable<int>? ParseIds(string? csv)
            => string.IsNullOrWhiteSpace(csv)
                ? null
                : csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                     .Select(int.Parse)
                     .ToArray();
    }
}
