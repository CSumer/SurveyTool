using SurveyTool.Api.Contracts.Responses;
using SurveyTool.Core.Application.Models;

namespace SurveyTool.Api.Mapping
{
    public static class SubmissionMappers
    {
        public static IEnumerable<AnswerSubmission> ToCore(this SubmitResponseDto dto) =>
            dto.Items.Select(i => new AnswerSubmission
            {
                QuestionId = i.QuestionId,
                SelectedOptionIds = i.SelectedOptionIds?.ToList(),
                FreeText = i.FreeText
            });
    }
}