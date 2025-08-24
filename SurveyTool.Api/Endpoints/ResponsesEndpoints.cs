using Microsoft.OpenApi.Any;
using SurveyTool.Api.Contracts.Responses;
using SurveyTool.Api.Mapping;
using SurveyTool.Core.Application.Interfaces;

namespace SurveyTool.Api.Endpoints
{
    public static class ResponsesEndpoints
    {
        public static IEndpointRouteBuilder MapResponseEndpoints(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/api")
                           .WithTags("Responses")
                           .WithOpenApi();

            // POST /api/surveys/{id}/responses

            group.MapPost("/surveys/{id:int}/responses",
                async (ISurveyService svc, int id, SubmitResponseDto dto) =>
                {
                    var (responseId, score) = await svc.SubmitResponseAsync(id, dto.ToCore());
                    return Results.Ok(new SubmitResponseResultDto(responseId, score));
                })
            .WithName("Responses_Submit")
            .WithSummary("Submit a response to a survey")
            .WithDescription("Validates visibility and types, computes the score, persists, and returns the response ID and score.")
            .Accepts<SubmitResponseDto>("application/json")
            .Produces<SubmitResponseResultDto>(StatusCodes.Status200OK)
            .ProducesValidationProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .WithOpenApi(op =>
            {
                op.OperationId = "Responses_Submit";
                op.Parameters[0].Description = "The survey identifier.";
                op.RequestBody.Content["application/json"].Example = new OpenApiString("""
                {
                  "items": [
                    { "questionId": 1, "selectedOptionIds": [11] },
                    { "questionId": 2, "selectedOptionIds": [21, 22] },
                    { "questionId": 3, "freeText": "Great service!" }
                  ]
                }
                """);
                
                op.Responses["200"].Content["application/json"].Example =
                    new OpenApiString("""{ "responseId": 301, "score": 9 }""");
                return op;
            });

            // GET /api/responses/{responseId}
            group.MapGet("/responses/{responseId:int}",
                async (ISurveyService svc, int responseId) =>
                {
                    var resp = await svc.GetResponseAsync(responseId);
                    return resp is null ? Results.NotFound() : Results.Ok(resp.ToDetailsDto());
                })
            .WithName("Responses_GetById")
            .WithSummary("Get a response by ID")
            .WithDescription("Returns a single response including items and the computed score.")
            .Produces<ResponseDetailsDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .WithOpenApi(op =>
            {
                op.OperationId = "Responses_GetById";
                op.Parameters[0].Description = "The response identifier.";
                op.Responses["200"].Content["application/json"].Example = new OpenApiString("""
            {
              "id": 301,
              "surveyId": 1,
              "createdAt": "2025-08-23T15:01:22Z",
              "score": 9,
              "items": [
                { "questionId": 1, "selectedOptionIds": [11], "freeText": null },
                { "questionId": 2, "selectedOptionIds": [21,22], "freeText": null },
                { "questionId": 3, "selectedOptionIds": null, "freeText": "Great service!" }
              ]
            }
            """);
                return op;
            });

            // GET /api/surveys/{surveyId}/responses
            group.MapGet("/surveys/{surveyId:int}/responses",
                async (ISurveyService svc, int surveyId) =>
                {
                    var list = await svc.ListResponsesForSurveyAsync(surveyId);
                    return Results.Ok(list.Select(r => r.ToSummaryDto()));
                })
            .WithName("Responses_ListForSurvey")
            .WithSummary("List responses for a survey")
            .WithDescription("Returns id, surveyId, createdAt, and score for each response submitted to the survey.")
            .Produces<IEnumerable<ResponseSummaryDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .WithOpenApi(op =>
            {
                op.OperationId = "Responses_ListForSurvey";
                op.Parameters[0].Description = "The survey identifier.";
                op.Responses["200"].Content["application/json"].Example = new OpenApiString("""
            [
              { "id": 301, "surveyId": 1, "createdAt": "2025-08-23T15:01:22Z", "score": 9 },
              { "id": 302, "surveyId": 1, "createdAt": "2025-08-23T15:03:05Z", "score": 5 }
            ]
            """);
                return op;
            });

            return app;
        }
    }
}