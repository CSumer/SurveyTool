namespace SurveyTool.Core.Domain
{
    public class Question
    {
        public int Id { get; set; }
        public int SurveyId { get; set; }
        public string Text { get; set; } = "";
        public QuestionType Type { get; set; }
        public int? ParentQuestionId { get; set; }
        public int[]? VisibilityShowWhenAnyOptionIds { get; set; }
        public List<AnswerOption> Options { get; set; } = [];
    }
}

public enum QuestionType { SingleChoice, MultipleChoice, FreeText }