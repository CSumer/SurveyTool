namespace SurveyTool.Core.Domain
{
    public class AnswerOption
    {
        public int Id { get; set; }
        public int QuestionId { get; set; }
        public string Text { get; set; } = "";
        public int Weight { get; set; }
    }
}