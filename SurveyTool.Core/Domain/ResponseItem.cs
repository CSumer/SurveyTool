namespace SurveyTool.Core.Domain
{
    public class ResponseItem
    {
        public int Id { get; set; }
        public int SurveyResponseId { get; set; }
        public int QuestionId { get; set; }
        public string? FreeText { get; set; }
        public string? SelectedOptionIdsCsv { get; set; }
    }
}