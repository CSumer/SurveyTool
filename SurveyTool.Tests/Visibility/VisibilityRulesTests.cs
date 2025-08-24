using FluentAssertions;
using SurveyTool.Core.Application.Exceptions;
using SurveyTool.Core.Application.Interfaces;
using SurveyTool.Core.Application.Models;
using SurveyTool.Infrastructure.Services;
using SurveyTool.Tests.Support;

namespace SurveyTool.Tests.Visibility;

[TestFixture]
[Category("Visibility")]
public class VisibilityRulesTests
{
    private static (ISurveyService svc, TestSeed.SeedIds ids) Build()
    {
        var db = TestFixture.NewContext();
        var ids = TestSeed.SeedBasicSurvey(db);
        var svc = new SurveyService(db);
        return (svc, ids);
    }

    [Test]
    public async Task ChildQuestion_IsRejected_WhenParentNotTriggering()
    {
        var (svc, ids) = Build();

        // Q1 = Very (does NOT trigger Q2), but attempt to answer Q2
        var items = new[]
        {
            new AnswerSubmission
            {
                QuestionId = ids.Q1,
                SelectedOptionIds = new[] { ids.Q1_Very },
                FreeText = null
            },
            new AnswerSubmission
            {
                QuestionId = ids.Q2,
                SelectedOptionIds = new[] { ids.Q2_Quality },
                FreeText = null
            }
        };

        var act = async () => await svc.SubmitResponseAsync(ids.SurveyId, items);

        await act.Should().ThrowAsync<DomainValidationException>()
                 .WithMessage($"*{ids.Q2}*not visible*");
    }

    [Test]
    public async Task ChildQuestion_IsAccepted_WhenParentTriggers()
    {
        var (svc, ids) = Build();

        // Q1 = Not (triggers Q2)
        var items = new[]
        {
            new AnswerSubmission
            {
                QuestionId = ids.Q1,
                SelectedOptionIds = new[] { ids.Q1_Not },
                FreeText = null
            },
            new AnswerSubmission
            {
                QuestionId = ids.Q2,
                SelectedOptionIds = new[] { ids.Q2_Support },
                FreeText = null
            }
        };

        var (_, score) = await svc.SubmitResponseAsync(ids.SurveyId, items);

        // Score = 1 (Q1 Not) + 2 (Support)
        score.Should().Be(3);
    }

    [Test]
    public async Task InvisibleQuestions_MayBeOmitted_AndSubmissionSucceeds()
    {
        var (svc, ids) = Build();

        // Q1 = Very → Q2 invisible → omit Q2 and still succeed
        var items = new[]
        {
            new AnswerSubmission
            {
                QuestionId = ids.Q1,
                SelectedOptionIds = new[] { ids.Q1_Very },
                FreeText = null
            }
            // no Q2 in payload
        };

        var (_, score) = await svc.SubmitResponseAsync(ids.SurveyId, items);
        score.Should().Be(5);
    }

    [Test]
    public async Task ChildQuestion_IsRejected_WhenParentNotAnsweredAtAll()
    {
        var (svc, ids) = Build();

        var items = new[]
        {
        new AnswerSubmission { QuestionId = ids.Q2, SelectedOptionIds = new[] { ids.Q2_Quality } }
    };

        var act = async () => await svc.SubmitResponseAsync(ids.SurveyId, items);
        await act.Should().ThrowAsync<DomainValidationException>()
                 .WithMessage($"*{ids.Q2}*not visible*");
    }

    [Test]
    public async Task SingleChoice_Rejects_MultipleSelectedOptions()
    {
        var (svc, ids) = Build();

        var items = new[]
        {
        new AnswerSubmission { QuestionId = ids.Q1, SelectedOptionIds = new[] { ids.Q1_Very, ids.Q1_Somewhat } }
    };

        var act = async () => await svc.SubmitResponseAsync(ids.SurveyId, items);
        await act.Should().ThrowAsync<DomainValidationException>()
         .WithMessage("*exactly one option*");

    }

    [Test]
    public async Task MultipleChoice_Rejects_DuplicateOptionIds()
    {
        var (svc, ids) = Build();

        var items = new[]
        {
        
        new AnswerSubmission
        {
            QuestionId = ids.Q1,
            SelectedOptionIds = new[] { ids.Q1_Not }
        },
        
        new AnswerSubmission
        {
            QuestionId = ids.Q2,
            SelectedOptionIds = new[] { ids.Q2_Quality, ids.Q2_Quality }
        }
    };

        var act = async () => await svc.SubmitResponseAsync(ids.SurveyId, items);
        await act.Should().ThrowAsync<DomainValidationException>()
                 .WithMessage("*duplicate*");
    }


    [Test]
    public async Task Rejects_OptionId_ThatDoesNotBelongToQuestion()
    {
        var (svc, ids) = Build();

        var items = new[]
        {
        new AnswerSubmission { QuestionId = ids.Q1, SelectedOptionIds = new[] { ids.Q1_Not } },
        new AnswerSubmission { QuestionId = ids.Q2, SelectedOptionIds = new[] { ids.Q1_Very } }
    };

        var act = async () => await svc.SubmitResponseAsync(ids.SurveyId, items);
        await act.Should().ThrowAsync<DomainValidationException>()
                 .WithMessage("*invalid*");
    }


    [Test]
    public async Task FreeText_Rejects_WhenOptionsProvided()
    {
        var (svc, ids) = Build();

        var items = new[]
        {
        new AnswerSubmission { QuestionId = ids.Q3, SelectedOptionIds = new[] { 123 }, FreeText = "text" }
    };

        var act = async () => await svc.SubmitResponseAsync(ids.SurveyId, items);
        await act.Should().ThrowAsync<DomainValidationException>()
                 .WithMessage("*freetext*");
    }


}
