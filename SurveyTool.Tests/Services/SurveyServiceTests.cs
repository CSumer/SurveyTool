using FluentAssertions;
using SurveyTool.Core.Application.Exceptions;
using SurveyTool.Core.Application.Interfaces;
using SurveyTool.Core.Application.Models;
using SurveyTool.Infrastructure.Services;
using SurveyTool.Tests.Support;

namespace SurveyTool.Tests.Services;

[TestFixture]
[Category("Services")]
public class SurveyServiceTests
{
    private static (ISurveyService svc, Infrastructure.Data.AppDbContext db, TestSeed.SeedIds ids) Build()
    {
        var db = TestFixture.NewContext();
        var ids = TestSeed.SeedBasicSurvey(db);
        var svc = new SurveyService(db);
        return (svc, db, ids);
    }

    [Test]
    public async Task SubmitResponse_SumsWeights_SingleAndMultipleChoice()
    {
        var (svc, _, ids) = Build();

        var items = new[]
        {
            new AnswerSubmission { QuestionId = ids.Q1, SelectedOptionIds = new[]{ ids.Q1_Not }, FreeText = null }, // 1
            new AnswerSubmission { QuestionId = ids.Q2, SelectedOptionIds = new[]{ ids.Q2_Quality, ids.Q2_Support }, FreeText = null }, // 2 + 2
            new AnswerSubmission { QuestionId = ids.Q3, SelectedOptionIds = null, FreeText = "Please improve" } // 0
        };

        var (responseId, score) = await svc.SubmitResponseAsync(ids.SurveyId, items);

        score.Should().Be(5);
        responseId.Should().BeGreaterThan(0);
    }

    [Test]
    public async Task AggregateScore_ReturnsSumCountAverage()
    {
        var (svc, _, ids) = Build();

        await svc.SubmitResponseAsync(ids.SurveyId, new[] { new AnswerSubmission { QuestionId = ids.Q1, SelectedOptionIds = new[] { ids.Q1_Very }, FreeText = null } });     // 5
        await svc.SubmitResponseAsync(ids.SurveyId, new[] { new AnswerSubmission { QuestionId = ids.Q1, SelectedOptionIds = new[] { ids.Q1_Somewhat }, FreeText = null } }); // 3

        var (total, count, avg) = await svc.GetSurveyAggregateScoreAsync(ids.SurveyId);

        total.Should().Be(8);
        count.Should().Be(2);
        avg.Should().BeApproximately(4.0, 1e-9);
    }

    [Test]
    public async Task UpdateOption_Works()
    {
        var (svc, db, ids) = Build();
        var optionId = ids.Q2_Quality;

        var ok = await svc.UpdateOptionAsync(optionId, "Better Quality", 7);
        ok.Should().BeTrue();

        var updated = await db.Options.FindAsync(optionId);
        updated!.Text.Should().Be("Better Quality");
        updated.Weight.Should().Be(7);
    }

    [Test]
    public async Task DeleteOption_Works()
    {
        var (svc, db, ids) = Build();
        var optionId = ids.Q2_Quality;

        var ok = await svc.DeleteOptionAsync(optionId);
        ok.Should().BeTrue();

        (await db.Options.FindAsync(optionId)).Should().BeNull();
    }

    [Test]
    public async Task UpdateOption_ReturnsFalse_WhenOptionNotFound()
    {
        var (svc, _, _) = Build();
        var ok = await svc.UpdateOptionAsync(optionId: 999999, text: "x", weight: 1);
        ok.Should().BeFalse();
    }

    [Test]
    public async Task DeleteOption_ReturnsFalse_WhenOptionNotFound()
    {
        var (svc, _, _) = Build();
        var ok = await svc.DeleteOptionAsync(optionId: 999999);
        ok.Should().BeFalse();
    }

    [Test]
    public async Task UpdateQuestion_AllowsChangingTextTypeAndParent()
    {
        var (svc, db, ids) = Build();

        // add a new parent in same survey
        var newParent = new SurveyTool.Core.Domain.Question
        {
            SurveyId = ids.SurveyId,
            Text = "Gate?",
            Type = QuestionType.SingleChoice
        };
        db.Questions.Add(newParent); await db.SaveChangesAsync();

        var ok = await svc.UpdateQuestionAsync(
            questionId: ids.Q2,
            text: "Updated improvements?",
            type: QuestionType.MultipleChoice,
            parentQuestionId: newParent.Id,
            showWhenAnyOptionSelected: null);

        ok.Should().BeTrue();

        var q2 = await db.Questions.FindAsync(ids.Q2);
        q2!.Text.Should().Be("Updated improvements?");
        q2.ParentQuestionId.Should().Be(newParent.Id);
        q2.VisibilityShowWhenAnyOptionIds.Should().BeNull();
    }

    [Test]
    public async Task DeleteQuestion_Throws_WhenHasChildren_AndCascadeFalse()
    {
        var (svc, db, ids) = Build();
        var q3 = await db.Questions.FindAsync(ids.Q3);
        q3!.ParentQuestionId = ids.Q2;
        await db.SaveChangesAsync();

        var act = async () => await svc.DeleteQuestionAsync(ids.Q2, cascade: false);
        await act.Should().ThrowAsync<DomainValidationException>()
                 .WithMessage("*has child questions*");
    }

    [Test]
    public async Task DeleteQuestion_DeletesRecursively_WhenCascadeTrue()
    {
        var (svc, db, ids) = Build();
        var q3 = await db.Questions.FindAsync(ids.Q3);
        q3!.ParentQuestionId = ids.Q2;
        await db.SaveChangesAsync();

        var ok = await svc.DeleteQuestionAsync(ids.Q2, cascade: true);
        ok.Should().BeTrue();
        (await db.Questions.FindAsync(ids.Q2)).Should().BeNull();
        (await db.Questions.FindAsync(ids.Q3)).Should().BeNull();
    }

    [Test]
    public async Task UpdateQuestion_Throws_WhenParentIsSelf()
    {
        var (svc, _, ids) = Build();

        var act = async () => await svc.UpdateQuestionAsync(
            questionId: ids.Q2,
            text: "x",
            type: QuestionType.MultipleChoice,
            parentQuestionId: ids.Q2, // self
            showWhenAnyOptionSelected: null);

        await act.Should().ThrowAsync<DomainValidationException>()
                 .WithMessage("*cannot be its own parent*");
    }

    [Test]
    public async Task UpdateQuestion_Throws_WhenParentInDifferentSurvey()
    {
        var (svc, db, ids) = Build();

        // create a new survey + a parent question there
        var otherSurvey = new SurveyTool.Core.Domain.Survey { Title = "Other" };
        db.Surveys.Add(otherSurvey); await db.SaveChangesAsync();
        var otherParent = new SurveyTool.Core.Domain.Question
        {
            SurveyId = otherSurvey.Id,
            Text = "Other Q",
            Type = QuestionType.SingleChoice
        };
        db.Questions.Add(otherParent); await db.SaveChangesAsync();

        var act = async () => await svc.UpdateQuestionAsync(
            questionId: ids.Q2,
            text: "x",
            type: QuestionType.MultipleChoice,
            parentQuestionId: otherParent.Id,
            showWhenAnyOptionSelected: null);

        await act.Should().ThrowAsync<DomainValidationException>()
                 .WithMessage("*must belong to the same survey*");
    }

    [Test]
    public async Task Scoring_Allows_NegativeWeights_ResultCanBeNegative()
    {
        var (svc, db, ids) = Build();

        var neg = new SurveyTool.Core.Domain.AnswerOption
        {
            QuestionId = ids.Q2,
            Text = "Penalty",
            Weight = -3
        };
        db.Options.Add(neg); await db.SaveChangesAsync();

        var items = new[]
        {
        new AnswerSubmission { QuestionId = ids.Q1, SelectedOptionIds = new[] { ids.Q1_Not } },    // 1
        new AnswerSubmission { QuestionId = ids.Q2, SelectedOptionIds = new[] { neg.Id } }         // -3
    };

        var (_, score) = await svc.SubmitResponseAsync(ids.SurveyId, items);
        score.Should().Be(-2); // 1 + (-3)
    }

    [Test]
    public async Task GetSurveyAggregateScore_ReturnsZeros_WhenNoResponses()
    {
        var (svc, _, ids) = Build();

        var (total, count, avg) = await svc.GetSurveyAggregateScoreAsync(ids.SurveyId);

        total.Should().Be(0);
        count.Should().Be(0);
        avg.Should().Be(0);
    }

    [Test]
    public async Task ListResponsesForSurvey_ReturnsEmpty_WhenNoneExist()
    {
        var (svc, _, ids) = Build();

        var list = await svc.ListResponsesForSurveyAsync(ids.SurveyId);

        list.Should().BeEmpty();
    }
}
