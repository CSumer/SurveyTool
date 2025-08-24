using SurveyTool.Core.Domain;
using SurveyTool.Infrastructure.Data;

namespace SurveyTool.Infrastructure.Seed
{
    public static class SeedData
    {
        public static void Initialize(AppDbContext db)
        {
            if (db.Surveys.Any()) return;

            // Helper for response items
            static ResponseItem MakeItem(int questionId, IEnumerable<int>? optionIds = null, string? freeText = null) =>
                new ResponseItem
                {
                    QuestionId = questionId,
                    SelectedOptionIdsCsv = optionIds is null ? null : string.Join(",", optionIds),
                    FreeText = freeText
                };

            // =====================================
            // SURVEY 1: Customer Satisfaction Survey
            // =====================================
            var survey1 = new Survey
            {
                Title = "Customer Satisfaction Survey",
                Description = "A demo survey for testing the API"
            };
            db.Surveys.Add(survey1);
            db.SaveChanges();

            // Q1: Single choice
            var s1_q1 = new Question
            {
                SurveyId = survey1.Id,
                Text = "How satisfied are you with our service?",
                Type = QuestionType.SingleChoice
            };
            db.Questions.Add(s1_q1);
            db.SaveChanges();

            var s1_q1opt1 = new AnswerOption { QuestionId = s1_q1.Id, Text = "Very satisfied", Weight = 5 };
            var s1_q1opt2 = new AnswerOption { QuestionId = s1_q1.Id, Text = "Somewhat satisfied", Weight = 3 };
            var s1_q1opt3 = new AnswerOption { QuestionId = s1_q1.Id, Text = "Not satisfied", Weight = 1 };
            db.Options.AddRange(s1_q1opt1, s1_q1opt2, s1_q1opt3);
            db.SaveChanges();

            // Q2: Multiple choice (conditional)
            var s1_q2 = new Question
            {
                SurveyId = survey1.Id,
                Text = "What areas need improvement? (Choose all that apply)",
                Type = QuestionType.MultipleChoice,
                ParentQuestionId = s1_q1.Id,
                VisibilityShowWhenAnyOptionIds = new[] { s1_q1opt3.Id }
            };
            db.Questions.Add(s1_q2);
            db.SaveChanges();

            var s1_q2opt1 = new AnswerOption { QuestionId = s1_q2.Id, Text = "Product quality", Weight = 2 };
            var s1_q2opt2 = new AnswerOption { QuestionId = s1_q2.Id, Text = "Customer support", Weight = 2 };
            var s1_q2opt3 = new AnswerOption { QuestionId = s1_q2.Id, Text = "Delivery time", Weight = 1 };
            db.Options.AddRange(s1_q2opt1, s1_q2opt2, s1_q2opt3);
            db.SaveChanges();

            // Q3: Free text
            var s1_q3 = new Question
            {
                SurveyId = survey1.Id,
                Text = "Any additional comments?",
                Type = QuestionType.FreeText
            };
            db.Questions.Add(s1_q3);
            db.SaveChanges();

            // Responses for Survey 1
            var s1_resp1 = new SurveyResponse
            {
                SurveyId = survey1.Id,
                CreatedAt = DateTime.UtcNow,
                Items =
                {
                    MakeItem(s1_q1.Id, new[] { s1_q1opt1.Id }),
                    MakeItem(s1_q3.Id, null, "Great service!")
                },
                Score = s1_q1opt1.Weight
            };
            db.Responses.Add(s1_resp1);
            db.SaveChanges();

            var s1_resp2 = new SurveyResponse
            {
                SurveyId = survey1.Id,
                CreatedAt = DateTime.UtcNow.AddMinutes(1),
                Items =
                {
                    MakeItem(s1_q1.Id, new[] { s1_q1opt3.Id }),
                    MakeItem(s1_q2.Id, new[] { s1_q2opt1.Id, s1_q2opt2.Id }),
                    MakeItem(s1_q3.Id, null, "Improve product quality and support response times.")
                },
                Score = s1_q1opt3.Weight + s1_q2opt1.Weight + s1_q2opt2.Weight
            };
            db.Responses.Add(s1_resp2);
            db.SaveChanges();

            // =====================================
            // SURVEY 2: Employee Engagement Survey
            // =====================================
            var survey2 = new Survey
            {
                Title = "Employee Engagement Survey",
                Description = "Collect feedback from employees on workplace engagement"
            };
            db.Surveys.Add(survey2);
            db.SaveChanges();

            // Q1: Single choice
            var s2_q1 = new Question
            {
                SurveyId = survey2.Id,
                Text = "How would you rate your overall job satisfaction?",
                Type = QuestionType.SingleChoice
            };
            db.Questions.Add(s2_q1);
            db.SaveChanges();

            var s2_q1opt1 = new AnswerOption { QuestionId = s2_q1.Id, Text = "Highly satisfied", Weight = 5 };
            var s2_q1opt2 = new AnswerOption { QuestionId = s2_q1.Id, Text = "Satisfied", Weight = 3 };
            var s2_q1opt3 = new AnswerOption { QuestionId = s2_q1.Id, Text = "Dissatisfied", Weight = 1 };
            db.Options.AddRange(s2_q1opt1, s2_q1opt2, s2_q1opt3);
            db.SaveChanges();

            // Q2: Free text
            var s2_q2 = new Question
            {
                SurveyId = survey2.Id,
                Text = "What improvements would you like to see?",
                Type = QuestionType.FreeText
            };
            db.Questions.Add(s2_q2);
            db.SaveChanges();

            // Response for Survey 2
            var s2_resp1 = new SurveyResponse
            {
                SurveyId = survey2.Id,
                CreatedAt = DateTime.UtcNow.AddMinutes(2),
                Items =
                {
                    MakeItem(s2_q1.Id, new[] { s2_q1opt2.Id }),
                    MakeItem(s2_q2.Id, null, "Better work-life balance and more team events.")
                },
                Score = s2_q1opt2.Weight
            };
            db.Responses.Add(s2_resp1);
            db.SaveChanges();
        }
    }
}
