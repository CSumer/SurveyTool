namespace SurveyTool.Core.Application.Models
{
    public sealed class AnswerSubmission
    {
        public int QuestionId { get; init; }
        public IReadOnlyCollection<int>? SelectedOptionIds { get; init; }
        public string? FreeText { get; init; }
    }
}