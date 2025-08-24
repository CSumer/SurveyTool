namespace SurveyTool.Core.Domain
{
    public class Survey
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public string? Description { get; set; }
        public List<Question> Questions { get; set; } = [];
    }
}