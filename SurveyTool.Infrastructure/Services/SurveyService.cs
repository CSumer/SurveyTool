using Microsoft.EntityFrameworkCore;
using SurveyTool.Core.Application.Exceptions;
using SurveyTool.Core.Application.Interfaces;
using SurveyTool.Core.Application.Models;
using SurveyTool.Core.Domain;
using SurveyTool.Infrastructure.Data;

namespace SurveyTool.Infrastructure.Services
{
    public sealed class SurveyService(AppDbContext db) : ISurveyService
    {
        public async Task<int> CreateSurveyAsync(string title, string? description)
        {
            var s = new Survey { Title = title, Description = description };
            db.Surveys.Add(s);
            await db.SaveChangesAsync();
            return s.Id;
        }

        public Task<Survey?> GetSurveyAsync(int id) =>
            db.Surveys
              .Include(s => s.Questions)
              .ThenInclude(q => q.Options)
              .FirstOrDefaultAsync(s => s.Id == id);

        public async Task<IReadOnlyList<Survey>> ListSurveysAsync()
        {
            return await db.Surveys
                .AsNoTracking()
                .OrderBy(s => s.Id)
                .ToListAsync();
        }

        public async Task<bool> UpdateSurveyAsync(int id, string title, string? description)
        {
            var s = await db.Surveys.FirstOrDefaultAsync(x => x.Id == id);
            if (s is null) return false;
            s.Title = title;
            s.Description = description;
            await db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteSurveyAsync(int id)
        {
            // Load survey; if not found → false
            var s = await db.Surveys.FirstOrDefaultAsync(x => x.Id == id);
            if (s is null) return false;

            db.Surveys.Remove(s);
            await db.SaveChangesAsync();
            return true;
        }


        public async Task<int> AddQuestionAsync(int surveyId, Question q)
        {
            // quick referential check
            var exists = await db.Surveys.AnyAsync(s => s.Id == surveyId);
            if (!exists) throw new DomainValidationException($"Survey {surveyId} not found.");

            q.SurveyId = surveyId;
            db.Questions.Add(q);
            await db.SaveChangesAsync();
            return q.Id;
        }

        public async Task<bool> UpdateQuestionAsync(int questionId, string text, QuestionType type,
    int? parentQuestionId, IEnumerable<int>? showWhenAnyOptionSelected)
        {
            var q = await db.Questions.FirstOrDefaultAsync(x => x.Id == questionId);
            if (q is null) return false;

            // Validate parent belongs to same survey (and avoid self/loop)
            if (parentQuestionId.HasValue)
            {
                if (parentQuestionId.Value == questionId)
                    throw new DomainValidationException("A question cannot be its own parent.");

                var parent = await db.Questions
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Id == parentQuestionId.Value);

                if (parent is null)
                    throw new DomainValidationException($"Parent question {parentQuestionId} not found.");

                if (parent.SurveyId != q.SurveyId)
                    throw new DomainValidationException("Parent question must belong to the same survey.");
            }

            q.Text = text;
            q.Type = type;
            q.ParentQuestionId = parentQuestionId;
            q.VisibilityShowWhenAnyOptionIds = showWhenAnyOptionSelected?.ToArray();

            await db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteQuestionAsync(int questionId, bool cascade = false)
        {
            var q = await db.Questions.FirstOrDefaultAsync(x => x.Id == questionId);
            if (q is null) return false;

            // find children
            var children = await db.Questions
                .Where(x => x.ParentQuestionId == questionId)
                .Select(x => x.Id)
                .ToListAsync();

            if (children.Count > 0 && !cascade)
                throw new DomainValidationException("Question has child questions. Delete with cascade=true or reparent them first.");

            if (cascade)
            {
                // recursive delete
                foreach (var childId in children)
                    await DeleteQuestionAsync(childId, cascade: true);
            }

            // Remove the question; cascade to options & response items by FK config
            db.Questions.Remove(q);
            await db.SaveChangesAsync();
            return true;
        }

        public async Task<int> AddOptionAsync(int questionId, AnswerOption opt)
        {
            var q = await db.Questions.FirstOrDefaultAsync(x => x.Id == questionId)
                    ?? throw new DomainValidationException($"Question {questionId} not found.");

            opt.QuestionId = q.Id;
            db.Options.Add(opt);
            await db.SaveChangesAsync();
            return opt.Id;
        }

        public async Task<bool> UpdateOptionAsync(int optionId, string text, int weight)
        {
            var opt = await db.Options.FirstOrDefaultAsync(x => x.Id == optionId);
            if (opt is null) return false;

            opt.Text = text;
            opt.Weight = weight;
            await db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteOptionAsync(int optionId)
        {
            var opt = await db.Options.FirstOrDefaultAsync(x => x.Id == optionId);
            if (opt is null) return false;

            db.Options.Remove(opt);
            await db.SaveChangesAsync();
            return true;
        }


        public async Task<(int responseId, int score)> SubmitResponseAsync(int surveyId, IEnumerable<AnswerSubmission> items)
        {
            // Load full survey graph
            var survey = await db.Surveys
                .Include(s => s.Questions)
                    .ThenInclude(q => q.Options)
                .FirstOrDefaultAsync(s => s.Id == surveyId)
                ?? throw new DomainValidationException($"Survey {surveyId} not found.");

            // Lookups
            var questionsById = survey.Questions.ToDictionary(q => q.Id);

            var dupQ = items.GroupBy(i => i.QuestionId).FirstOrDefault(g => g.Count() > 1)?.Key;
            if (dupQ is int dq)
                throw new DomainValidationException($"Duplicate question {dq} in submission.");
            var submissionByQ = items.ToDictionary(i => i.QuestionId);

            // ---- Visibility helper (supports multi-level parents) ----
            var visibleMemo = new Dictionary<int, bool>();
            bool IsVisible(Question q)
            {
                if (visibleMemo.TryGetValue(q.Id, out var v)) return v;

                // No rule => visible
                if (q.ParentQuestionId is null)
                    return visibleMemo[q.Id] = true;

                // Parent must exist, be visible, and be answered with a triggering option
                if (!questionsById.TryGetValue(q.ParentQuestionId.Value, out var parent))
                    return visibleMemo[q.Id] = false;

                if (!IsVisible(parent)) // recursion up the chain
                    return visibleMemo[q.Id] = false;

                if (!submissionByQ.TryGetValue(parent.Id, out var parentAns))
                    return visibleMemo[q.Id] = false; // parent not answered => child hidden

                var chosen = parentAns.SelectedOptionIds ?? Array.Empty<int>();
                var triggers = q.VisibilityShowWhenAnyOptionIds ?? Array.Empty<int>();
                var show = chosen.Intersect(triggers).Any();
                return visibleMemo[q.Id] = show;
            }

            // ---- Validate answers & compute score ----
            int score = 0;
            var response = new SurveyResponse { SurveyId = surveyId, CreatedAt = DateTime.UtcNow };

            foreach (var item in items)
            {
                if (!questionsById.TryGetValue(item.QuestionId, out var q))
                    throw new DomainValidationException($"Question {item.QuestionId} does not belong to survey {surveyId}.");

                // Enforce "only accept answers for visible questions"
                if (!IsVisible(q))
                    throw new DomainValidationException($"Question {q.Id} is not visible under current answers.");

                switch (q.Type)
                {
                    case QuestionType.FreeText:
                        {
                            // ❗ Reject options on freetext
                            if (item.SelectedOptionIds is not null && item.SelectedOptionIds.Any())
                                throw new DomainValidationException($"Question {q.Id} is freetext; options are not allowed.");

                            // Free text contributes 0 to score
                            response.Items.Add(new ResponseItem
                            {
                                QuestionId = q.Id,
                                FreeText = item.FreeText
                            });
                            break;
                        }

                    case QuestionType.SingleChoice:
                        {
                            var ids = (item.SelectedOptionIds ?? Array.Empty<int>()).ToList();
                            if (ids.Count != 1)
                                throw new DomainValidationException($"Question {q.Id} requires exactly one option.");

                            var opt = q.Options.FirstOrDefault(o => o.Id == ids[0])
                                      ?? throw new DomainValidationException($"Invalid option for question {q.Id}.");
                            response.Items.Add(new ResponseItem
                            {
                                QuestionId = q.Id,
                                SelectedOptionIdsCsv = string.Join(",", ids)
                            });
                            score += opt.Weight;
                            break;
                        }

                    case QuestionType.MultipleChoice:
                        {
                            var raw = item.SelectedOptionIds ?? Array.Empty<int>();
                            if (!raw.Any())
                                throw new DomainValidationException($"Question {q.Id} requires at least one option.");

                            // ❗ Detect duplicate option IDs
                            var dup = raw.GroupBy(x => x).FirstOrDefault(g => g.Count() > 1)?.Key;
                            if (dup is int d)
                                throw new DomainValidationException($"Question {q.Id} has duplicate option id {d}.");

                            // Now it's safe to de-dupe (or skip this line entirely since we just proved no dups)
                            var ids = raw.Distinct().ToList();

                            // Validate all options belong to this question
                            var valid = q.Options.Where(o => ids.Contains(o.Id)).ToList();
                            if (valid.Count != ids.Count)
                                throw new DomainValidationException($"One or more selected options are invalid for question {q.Id}.");

                            response.Items.Add(new ResponseItem
                            {
                                QuestionId = q.Id,
                                SelectedOptionIdsCsv = string.Join(",", ids)
                            });
                            score += valid.Sum(v => v.Weight);
                            break;
                        }


                    default:
                        throw new DomainValidationException($"Unsupported question type for question {q.Id}.");
                }
            }

            //todo: Optional: can auto-reject if a visible question is left unanswered.
            // Requirement only says "accept answers for visible questions" (and reject for invisible),
            // so leaving visibles unanswered is allowed here.

            response.Score = score;
            db.Responses.Add(response);
            await db.SaveChangesAsync();

            return (response.Id, score);
        }

        public async Task<SurveyResponse?> GetResponseAsync(int responseId) =>
    await db.Responses
            .Include(r => r.Items)
            .FirstOrDefaultAsync(r => r.Id == responseId);

        public async Task<IReadOnlyList<SurveyResponse>> ListResponsesForSurveyAsync(int surveyId) =>
            await db.Responses
                    .Where(r => r.SurveyId == surveyId)
                    .OrderByDescending(r => r.CreatedAt)
                    .ToListAsync();

        public async Task<(int totalScore, int responseCount, double averageScore)> GetSurveyAggregateScoreAsync(int surveyId)
        {
            var query = db.Responses.AsNoTracking().Where(r => r.SurveyId == surveyId);

            var responseCount = await query.CountAsync();
            if (responseCount == 0)
                return (0, 0, 0d);

            var totalScore = await query.SumAsync(r => r.Score);
            var average = (double)totalScore / responseCount;

            return (totalScore, responseCount, average);
        }


    }
}