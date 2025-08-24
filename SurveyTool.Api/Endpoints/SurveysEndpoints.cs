using SurveyTool.Api.Contracts.Surveys;
using SurveyTool.Api.Infrastructure.Validation;
using SurveyTool.Api.Mapping;
using SurveyTool.Core.Application.Interfaces;

namespace SurveyTool.Api.Endpoints
{
    public static class SurveysEndpoints
    {
        public static IEndpointRouteBuilder MapSurveyEndpoints(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/api/surveys")
               .WithTags("Surveys")
               .WithOpenApi();

            // GET /api/surveys/{id}
            group.MapGet("{id:int}", async (ISurveyService svc, int id) =>
            {
                var survey = await svc.GetSurveyAsync(id);
                return survey is null ? Results.NotFound() : Results.Ok(survey.ToDto());
            });

            // GET /api/surveys (list all surveys)
            group.MapGet("",
            async (ISurveyService svc) =>
            {
                var surveys = await svc.ListSurveysAsync();
                return Results.Ok(surveys.Select(s => s.ToDto()));
            })
            .WithSummary("List all surveys")
            .Produces<IEnumerable<SurveyDto>>(StatusCodes.Status200OK);

            // GET /api/surveys/{surveyId}/score
            group.MapGet("{surveyId:int}/score",
                async (ISurveyService svc, int surveyId) =>
                {
                    // Optionally 404 if survey doesn't exist
                    var survey = await svc.GetSurveyAsync(surveyId);
                    if (survey is null) return Results.NotFound();

                    var (total, count, avg) = await svc.GetSurveyAggregateScoreAsync(surveyId);
                    return Results.Ok(new SurveyScoreSummaryDto(surveyId, total, count, avg));
                })
                .WithSummary("Get aggregate score for a survey")
                .WithDescription("Returns the sum of scores across all responses for the survey, along with response count and average.")
                .Produces<SurveyScoreSummaryDto>(StatusCodes.Status200OK)
                .ProducesProblem(StatusCodes.Status404NotFound);


            // POST /api/surveys
            group.MapPost("", async (ISurveyService svc, CreateSurveyDto dto) =>
            {
                var id = await svc.CreateSurveyAsync(dto.Title, dto.Description);
                return Results.Created($"/api/surveys/{id}", new { id });
            })
            .AddEndpointFilter(new ValidationFilter<CreateSurveyDto>());

            // PUT /api/surveys/{id}
            group.MapPut("{id:int}", async (ISurveyService svc, int id, UpdateSurveyDto dto) =>
            {
                var ok = await svc.UpdateSurveyAsync(id, dto.Title, dto.Description);
                return ok ? Results.NoContent() : Results.NotFound();
            })
            .AddEndpointFilter(new ValidationFilter<UpdateSurveyDto>())
            .WithSummary("Update a survey")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesValidationProblem();

            // DELETE /api/surveys/{id}
            group.MapDelete("{id:int}", async (ISurveyService svc, int id) =>
            {
                var ok = await svc.DeleteSurveyAsync(id);
                return ok ? Results.NoContent() : Results.NotFound();
            })
            .WithSummary("Delete a survey (and its dependent data)")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);

            return app;
        }
    }
}