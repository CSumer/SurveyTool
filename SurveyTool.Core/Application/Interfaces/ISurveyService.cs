using SurveyTool.Core.Application.Models;
using SurveyTool.Core.Domain;

namespace SurveyTool.Core.Application.Interfaces
{
    /// <summary>
    /// Application-facing contract for working with Surveys, Questions, Options, and Responses.
    /// Implements validation (visibility, type rules), scoring, and simple CRUD operations.
    /// </summary>
    public interface ISurveyService
    {
        /// <summary>
        /// Creates a new survey.
        /// </summary>
        /// <param name="title">Survey title (required).</param>
        /// <param name="description">Optional description.</param>
        /// <returns>The newly created survey identifier.</returns>
        Task<int> CreateSurveyAsync(string title, string? description);

        /// <summary>
        /// Retrieves a survey (including its questions and options), or <c>null</c> if not found.
        /// </summary>
        /// <param name="id">Survey identifier.</param>
        /// <returns>The survey or <c>null</c>.</returns>
        Task<Survey?> GetSurveyAsync(int id);

        /// <summary>
        /// Lists all surveys (lightweight projection; typically without responses).
        /// </summary>
        /// <returns>All surveys in creation order.</returns>
        Task<IReadOnlyList<Survey>> ListSurveysAsync();

        /// <summary>
        /// Updates a survey's basic metadata.
        /// </summary>
        /// <param name="id">Survey identifier.</param>
        /// <param name="title">New title.</param>
        /// <param name="description">New (optional) description.</param>
        /// <returns><c>true</c> if the survey existed and was updated; otherwise <c>false</c>.</returns>
        Task<bool> UpdateSurveyAsync(int id, string title, string? description);

        /// <summary>
        /// Deletes a survey (and, per data model cascade rules, its questions/options/responses).
        /// </summary>
        /// <param name="id">Survey identifier.</param>
        /// <returns><c>true</c> if the survey existed and was deleted; otherwise <c>false</c>.</returns>
        Task<bool> DeleteSurveyAsync(int id);

        /// <summary>
        /// Adds a question to an existing survey.
        /// </summary>
        /// <param name="surveyId">Target survey identifier.</param>
        /// <param name="question">Question to add. Its <see cref="Question.SurveyId"/> will be set.</param>
        /// <returns>The newly created question identifier.</returns>
        /// <exception cref="SurveyTool.Core.Application.Exceptions.DomainValidationException">
        /// Thrown when the survey does not exist.
        /// </exception>
        Task<int> AddQuestionAsync(int surveyId, Question question);

        /// <summary>
        /// Updates question text, type, parent, and visibility rules.
        /// </summary>
        /// <param name="questionId">Question identifier.</param>
        /// <param name="text">New question text.</param>
        /// <param name="type">Question type.</param>
        /// <param name="parentQuestionId">Optional parent question id (for conditional visibility).</param>
        /// <param name="showWhenAnyOptionSelected">
        /// Option ids on the parent that make this question visible (OR semantics).
        /// </param>
        /// <returns><c>true</c> if the question existed and was updated; otherwise <c>false</c>.</returns>
        /// <exception cref="SurveyTool.Core.Application.Exceptions.DomainValidationException">
        /// Thrown when parent equals the question itself, parent is missing, or parent belongs to a different survey.
        /// </exception>
        Task<bool> UpdateQuestionAsync(int questionId, string text, QuestionType type,
            int? parentQuestionId, IEnumerable<int>? showWhenAnyOptionSelected);

        /// <summary>
        /// Deletes the question.
        /// </summary>
        /// <param name="questionId">Question identifier.</param>
        /// <param name="cascade">
        /// When <c>true</c>, deletes child questions (and their options) recursively.
        /// When <c>false</c> and children exist, a validation exception is thrown.
        /// </param>
        /// <returns><c>true</c> if the question existed and was deleted; otherwise <c>false</c>.</returns>
        /// <exception cref="SurveyTool.Core.Application.Exceptions.DomainValidationException">
        /// Thrown when <paramref name="cascade"/> is <c>false</c> and the question has children.
        /// </exception>
        Task<bool> DeleteQuestionAsync(int questionId, bool cascade = false);

        /// <summary>
        /// Adds an option to an existing question.
        /// </summary>
        /// <param name="questionId">Target question identifier.</param>
        /// <param name="option">Option to add. Its <see cref="AnswerOption.QuestionId"/> will be set.</param>
        /// <returns>The newly created option identifier.</returns>
        /// <exception cref="SurveyTool.Core.Application.Exceptions.DomainValidationException">
        /// Thrown when the question does not exist.
        /// </exception>
        Task<int> AddOptionAsync(int questionId, AnswerOption option);

        /// <summary>
        /// Updates an answer option's text and weight.
        /// </summary>
        /// <param name="optionId">Option identifier.</param>
        /// <param name="text">New display text.</param>
        /// <param name="weight">New weight (may be negative or zero).</param>
        /// <returns><c>true</c> if the option existed and was updated; otherwise <c>false</c>.</returns>
        Task<bool> UpdateOptionAsync(int optionId, string text, int weight);

        /// <summary>
        /// Deletes an answer option.
        /// </summary>
        /// <param name="optionId">Option identifier.</param>
        /// <returns><c>true</c> if the option existed and was deleted; otherwise <c>false</c>.</returns>
        Task<bool> DeleteOptionAsync(int optionId);

        /// <summary>
        /// Validates visibility rules and per-type constraints, computes score from selected option weights,
        /// persists the response and its items, and returns identifiers and score.
        /// </summary>
        /// <param name="surveyId">Survey being answered.</param>
        /// <param name="items">Answer items keyed by question.</param>
        /// <returns>
        /// A tuple <c>(responseId, score)</c> where:
        /// <list type="bullet">
        /// <item><description><c>responseId</c> is the persisted response identifier;</description></item>
        /// <item><description><c>score</c> is the sum of selected option weights (FreeText contributes 0).</description></item>
        /// </list>
        /// </returns>
        /// <exception cref="SurveyTool.Core.Application.Exceptions.DomainValidationException">
        /// Thrown for invalid survey/question ids, invisible questions, invalid/duplicate options,
        /// or type rule violations.
        /// </exception>
        Task<(int responseId, int score)> SubmitResponseAsync(int surveyId, IEnumerable<AnswerSubmission> items);

        /// <summary>
        /// Retrieves a previously submitted response with its items, or <c>null</c> if not found.
        /// </summary>
        /// <param name="responseId">Response identifier.</param>
        /// <returns>The response or <c>null</c>.</returns>
        Task<SurveyResponse?> GetResponseAsync(int responseId);

        /// <summary>
        /// Lists responses for a survey in reverse chronological order.
        /// </summary>
        /// <param name="surveyId">Survey identifier.</param>
        /// <returns>Responses ordered by <see cref="SurveyResponse.CreatedAt"/> descending.</returns>
        Task<IReadOnlyList<SurveyResponse>> ListResponsesForSurveyAsync(int surveyId);

        /// <summary>
        /// Computes aggregate scoring metrics for a survey based on stored responses.
        /// </summary>
        /// <param name="surveyId">Survey identifier.</param>
        /// <returns>
        /// A tuple <c>(totalScore, responseCount, averageScore)</c>. When no responses exist, all values are zero.
        /// </returns>
        Task<(int totalScore, int responseCount, double averageScore)> GetSurveyAggregateScoreAsync(int surveyId);
    }
}
