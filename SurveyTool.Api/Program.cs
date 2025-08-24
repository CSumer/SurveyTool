using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using SurveyTool.Api.Endpoints;
using SurveyTool.Core.Application.Interfaces;
using SurveyTool.Infrastructure.Data;
using SurveyTool.Infrastructure.Seed;
using SurveyTool.Infrastructure.Services;
using SurveyTool.Core.Domain;
using Microsoft.AspNetCore.Diagnostics;
using System.Text.Json.Serialization;

// builder
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();

// Swagger / OpenAPI
builder.Services.AddSwaggerGen(o =>
{
    o.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Survey Tool API",
        Version = "v1",
        Description = "REST API for building surveys with conditional questions, responses, and scoring.",
        Contact = new OpenApiContact { Name = "Christopher Sumer", Email = "Chris.Sumer@gmail.com" },
    });

    o.UseInlineDefinitionsForEnums();

    // Include XML docs
    void IncludeXmlFor<T>()
    {
        var asm = typeof(T).Assembly;
        var xml = $"{asm.GetName().Name}.xml";
        var path = Path.Combine(AppContext.BaseDirectory, xml);
        if (File.Exists(path))
            o.IncludeXmlComments(path, includeControllerXmlComments: true);
    }
    IncludeXmlFor<Program>();   // SurveyTool.Api.xml
    IncludeXmlFor<Survey>();    // SurveyTool.Core.xml (ensure GenerateDocumentationFile=true in Core csproj)
});

// JSON: enums as strings
builder.Services.ConfigureHttpJsonOptions(opt =>
{
    opt.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Services.AddProblemDetails();

// Entity Framework In-Memory DB and Dependency Injection for services
builder.Services.AddDbContext<AppDbContext>(o => o.UseInMemoryDatabase("SurveyDb"));
builder.Services.AddScoped<ISurveyService, SurveyService>();

var app = builder.Build();

// Exception handling
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var feature = context.Features.Get<IExceptionHandlerPathFeature>();
        var ex = feature?.Error;

        if (ex is SurveyTool.Core.Application.Exceptions.DomainValidationException dve)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            var errors = new Dictionary<string, string[]>
            {
                [""] = new[] { dve.Message }
            };
            await Results.ValidationProblem(errors).ExecuteAsync(context);
            return;
        }

        await Results.Problem(statusCode: StatusCodes.Status500InternalServerError)
            .ExecuteAsync(context);
    });
});

// Dev-only: seed + Swagger UI polish
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
    SeedData.Initialize(db);

    app.UseSwagger();
    app.UseSwaggerUI(ui =>
    {
        ui.DocumentTitle = "Survey Tool – API Docs";
        ui.DisplayRequestDuration();
        ui.DefaultModelsExpandDepth(-1);
        ui.SwaggerEndpoint("/swagger/v1/swagger.json", "Survey Tool API v1");

        app.UseSwaggerUI(ui =>
        {
            ui.DocumentTitle = "Survey Tool – API Docs";
            ui.DisplayRequestDuration();
            ui.DefaultModelsExpandDepth(-1);
            ui.SwaggerEndpoint("/swagger/v1/swagger.json", "Survey Tool API v1");

            ui.ConfigObject.AdditionalItems["tagsSorter"] = "alpha";
            ui.ConfigObject.AdditionalItems["operationsSorter"] = "alpha";
        });

    });
}

app.UseHttpsRedirection();

// endpoints
app.MapSurveyEndpoints()
   .MapQuestionEndpoints()
   .MapResponseEndpoints();

app.Run();
