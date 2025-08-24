using SurveyTool.Api.Contracts.Options;
using SurveyTool.Api.Contracts.Questions;
using SurveyTool.Api.Contracts.Surveys;
using SurveyTool.Core.Domain;

namespace SurveyTool.Api.Mapping
{
    public static class DtoMappers
    {
        public static SurveyDto ToDto(this Survey s) =>
            new(
                s.Id,
                s.Title,
                s.Description,
                s.Questions.Select(q => q.ToDto())
            );

        public static QuestionDto ToDto(this Question q) =>
            new(
                q.Id,
                q.Text,
                q.Type,
                q.ParentQuestionId,
                q.VisibilityShowWhenAnyOptionIds,
                q.Options.Select(o => o.ToDto())
            );

        public static AnswerOptionDto ToDto(this AnswerOption o) =>
            new(o.Id, o.Text, o.Weight);

        public static Question ToDomain(this CreateQuestionDto dto, int surveyId) =>
            new()
            {
                SurveyId = surveyId,
                Text = dto.Text,
                Type = dto.Type,
                ParentQuestionId = dto.ParentQuestionId,
                VisibilityShowWhenAnyOptionIds = dto.ShowWhenAnyOptionSelected?.ToArray()
            };
    }
}