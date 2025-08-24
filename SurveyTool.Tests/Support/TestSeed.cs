using SurveyTool.Core.Domain;
using SurveyTool.Infrastructure.Data;

namespace SurveyTool.Tests.Support;

public static class TestSeed
{
    public sealed record SeedIds(
        int SurveyId,
        int Q1, int Q2, int Q3,
        int Q1_Very, int Q1_Somewhat, int Q1_Not,
        int Q2_Quality, int Q2_Support, int Q2_Delivery);

    public static SeedIds SeedBasicSurvey(AppDbContext db)
    {
        var survey = new Survey { Title = "CSAT", Description = "Test" };
        db.Surveys.Add(survey);
        db.SaveChanges();

        var q1 = new Question { SurveyId = survey.Id, Text = "Satisfaction?", Type = QuestionType.SingleChoice };
        db.Questions.Add(q1); db.SaveChanges();

        var opt1 = new AnswerOption { QuestionId = q1.Id, Text = "Very", Weight = 5 };
        var opt2 = new AnswerOption { QuestionId = q1.Id, Text = "Somewhat", Weight = 3 };
        var opt3 = new AnswerOption { QuestionId = q1.Id, Text = "Not", Weight = 1 };
        db.Options.AddRange(opt1, opt2, opt3); db.SaveChanges();

        var q2 = new Question
        {
            SurveyId = survey.Id,
            Text = "Improvements?",
            Type = QuestionType.MultipleChoice,
            ParentQuestionId = q1.Id,
            VisibilityShowWhenAnyOptionIds = new[] { opt3.Id }
        };
        db.Questions.Add(q2); db.SaveChanges();

        var q2o1 = new AnswerOption { QuestionId = q2.Id, Text = "Quality", Weight = 2 };
        var q2o2 = new AnswerOption { QuestionId = q2.Id, Text = "Support", Weight = 2 };
        var q2o3 = new AnswerOption { QuestionId = q2.Id, Text = "Delivery", Weight = 1 };
        db.Options.AddRange(q2o1, q2o2, q2o3); db.SaveChanges();

        var q3 = new Question { SurveyId = survey.Id, Text = "Comments", Type = QuestionType.FreeText };
        db.Questions.Add(q3); db.SaveChanges();

        return new SeedIds(survey.Id, q1.Id, q2.Id, q3.Id,
                           opt1.Id, opt2.Id, opt3.Id,
                           q2o1.Id, q2o2.Id, q2o3.Id);
    }
}
