using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using SurveyTool.Api.Contracts.Common;
using SurveyTool.Api.Contracts.Options;
using SurveyTool.Api.Contracts.Questions;
using SurveyTool.Api.Infrastructure.Validation;
using SurveyTool.Api.Mapping;
using SurveyTool.Core.Application.Interfaces;
using SurveyTool.Core.Domain;

namespace SurveyTool.Api.Endpoints
{
    public static class QuestionsEndpoints
    {
        public static IEndpointRouteBuilder MapQuestionEndpoints(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/api")
                           .WithTags("Questions")
                           .WithOpenApi();

            // POST /api/surveys/{id}/questions
            group.MapPost("/surveys/{id:int}/questions",
                async (ISurveyService svc, int id, CreateQuestionDto dto) =>
                {
                    var q = dto.ToDomain(id);
                    var qId = await svc.AddQuestionAsync(id, q);
                    return Results.Created($"/api/questions/{qId}", new { id = qId });
                })
            .AddEndpointFilter(new ValidationFilter<CreateQuestionDto>())
            .WithName("Questions_Create")
            .WithSummary("Add a question to a survey")
            .WithDescription("Creates a question (SingleChoice, MultipleChoice, or FreeText). Supports conditional visibility via parent/trigger options.")
            .Accepts<CreateQuestionDto>("application/json")
            .Produces<CreatedResultDto>(StatusCodes.Status201Created)
            .ProducesValidationProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .WithOpenApi(op =>
            {
                op.OperationId = "Questions_Create";

                op.Parameters[0].Description = "The survey identifier to attach the question to.";
                op.RequestBody.Content["application/json"].Example = new OpenApiString("""
            {
              "text": "How satisfied are you with our service?",
              "type": "SingleChoice",
              "parentQuestionId": null,
              "showWhenAnyOptionSelected": null
            }
            """);
                op.Responses["201"].Content["application/json"].Example = new OpenApiString("""{ "id": 101 }""");
                return op;
            });

            // PUT /api/questions/{questionId}
            group.MapPut("/questions/{questionId:int}",
            async (ISurveyService svc, int questionId, UpdateQuestionDto dto) =>
            {
                var ok = await svc.UpdateQuestionAsync(
                    questionId, dto.Text, dto.Type, dto.ParentQuestionId, dto.ShowWhenAnyOptionSelected);
                return ok ? Results.NoContent() : Results.NotFound();
            })
        .AddEndpointFilter(new ValidationFilter<UpdateQuestionDto>())
        .WithName("Questions_Update")
        .WithSummary("Update a question")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesValidationProblem(StatusCodes.Status400BadRequest)
        .WithOpenApi(op =>
        {
            op.OperationId = "Questions_Update";
            op.Parameters[0].Description = "The question identifier.";
            op.RequestBody.Content["application/json"] = new OpenApiMediaType
            {
                Example = new OpenApiString("""
                {
                  "text": "Updated question text?",
                  "type": "SingleChoice",
                  "parentQuestionId": null,
                  "showWhenAnyOptionSelected": null
                }
                """)
            };
            return op;
        });

            // DELETE /api/questions/{questionId}?cascade=true
            group.MapDelete("/questions/{questionId:int}",
                async (ISurveyService svc, int questionId, bool? cascade) =>
                {
                    var ok = await svc.DeleteQuestionAsync(questionId, cascade == true);
                    return ok ? Results.NoContent() : Results.NotFound();
                })
            .WithName("Questions_Delete")
            .WithSummary("Delete a question")
            .WithDescription("Deletes the question. If it has child questions, pass ?cascade=true to delete them recursively.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesValidationProblem(StatusCodes.Status400BadRequest)
            .WithOpenApi(op =>
            {
                op.OperationId = "Questions_Delete";
                op.Parameters[0].Description = "The question identifier.";
                var pCascade = op.Parameters.FirstOrDefault(p => p.Name == "cascade");
                if (pCascade is not null)
                {
                    pCascade.Description = "Delete child questions recursively if true.";
                    pCascade.Schema ??= new OpenApiSchema { Type = "boolean" };
                    pCascade.Example = new OpenApiBoolean(false);
                }
                return op;
            });

            // POST /api/questions/{questionId}/options
            group.MapPost("/questions/{questionId:int}/options",
                async (ISurveyService svc, int questionId, CreateAnswerOptionDto dto) =>
                {
                    var opt = new AnswerOption { QuestionId = questionId, Text = dto.Text, Weight = dto.Weight };
                    var optId = await svc.AddOptionAsync(questionId, opt);
                    return Results.Created($"/api/options/{optId}", new { id = optId });
                })
            .AddEndpointFilter(new ValidationFilter<CreateAnswerOptionDto>())
            .WithName("Options_Create")
            .WithSummary("Add an answer option to a question")
            .WithDescription("Creates an answer option with a weight used for scoring. Weight may be zero (optional effect) or negative if desired.")
            .Accepts<CreateAnswerOptionDto>("application/json")
            .Produces<CreatedResultDto>(StatusCodes.Status201Created)
            .ProducesValidationProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .WithOpenApi(op =>
            {
                op.OperationId = "Options_Create";
                op.Parameters[0].Description = "The question identifier to attach the option to.";
                op.RequestBody.Content["application/json"].Example = new OpenApiString("""
            { "text": "Very satisfied", "weight": 5 }
            """);
                op.Responses["201"].Content["application/json"].Example = new OpenApiString("""{ "id": 201 }""");
                return op;
            });

            // PUT /api/options/{optionId}
            group.MapPut("/options/{optionId:int}",
                async (ISurveyService svc, int optionId, UpdateAnswerOptionDto dto) =>
                {
                    var ok = await svc.UpdateOptionAsync(optionId, dto.Text, dto.Weight);
                    return ok ? Results.NoContent() : Results.NotFound();
                })
            .AddEndpointFilter(new ValidationFilter<UpdateAnswerOptionDto>())
            .WithName("Options_Update")
            .WithSummary("Update an answer option")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesValidationProblem(StatusCodes.Status400BadRequest)
            .WithOpenApi(op =>
            {
                op.OperationId = "Options_Update";
                op.Parameters[0].Description = "The option identifier.";
                op.RequestBody.Content["application/json"] = new OpenApiMediaType
                {
                    Example = new OpenApiString("""{ "text": "Very satisfied", "weight": 6 }""")
                };
                return op;
            });

            // DELETE /api/options/{optionId}
            group.MapDelete("/options/{optionId:int}",
                async (ISurveyService svc, int optionId) =>
                {
                    var ok = await svc.DeleteOptionAsync(optionId);
                    return ok ? Results.NoContent() : Results.NotFound();
                })
            .WithName("Options_Delete")
            .WithSummary("Delete an answer option")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .WithOpenApi(op =>
            {
                op.OperationId = "Options_Delete";
                op.Parameters[0].Description = "The option identifier.";
                return op;
            });

            return app;
        }
    }

}