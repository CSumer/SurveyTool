namespace SurveyTool.Core.Domain
{
    public class SurveyResponse
    {
        public int Id { get; set; }
        public int SurveyId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public int Score { get; set; }
        public List<ResponseItem> Items { get; set; } = [];
    }
}