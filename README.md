# Survey Tool API (ASP.NET Core 8)
A minimal, clean-architecture REST API for building surveys with conditional questions, accepting responses, and scoring based on weighted answers.

## Tech
- .NET 8 Minimal APIs
- EF Core InMemory (no external DB required)
- Swagger / OpenAPI (Swashbuckle 6.x)
- Three projects: `SurveyTool.Api`, `SurveyTool.Core`, `SurveyTool.Infrastructure`


## Quick start
dotnet restore
dotnet build

Run (Kestrel)
HTTPS profile: https://localhost:7201/swagger
HTTP profile: http://localhost:5295/swagger
The app seeds demo data in Development automatically.

## Visual Studio
Set SurveyTool.Api as Startup Project.
Choose https or http profile and press F5.

## API Overview
Surveys
	GET /api/surveys – list all
	GET /api/surveys/{id} – get one
	POST /api/surveys – create
	PUT /api/surveys/{id} – update
	DELETE /api/surveys/{id} – delete (cascades)
	GET /api/surveys/{id}/score – aggregate score (sum/count/average)

Questions & Options
	POST /api/surveys/{id}/questions – add question
	POST /api/questions/{questionId}/options – add option

Responses
	POST /api/surveys/{id}/responses – submit response (validates visibility & types; computes and persists score)
	GET /api/responses/{responseId} – get response details (items + score)
	GET /api/surveys/{surveyId}/responses – list responses for a survey

Swagger shows examples, summaries, and enum names instead of numbers (for clarity and readability).

## Conditional visibility
A question may specify:
	ParentQuestionId: the question it depends on
	VisibilityShowWhenAnyOptionIds: the set of parent option IDs that make it visible

When submitting responses:
	Children are only accepted if visible under the currently selected parent options.
	Invisible questions must be omitted from the payload.

## Scoring
Each option has an integer Weight (can be 0 or negative if desired).
Score = sum of all selected option weights across the response.
FreeText contributes 0.
Score is computed at submit time and stored at SurveyResponse.Score.
GET /api/surveys/{id}/score sums stored scores for fast aggregates.

## Seeding
In Development:
	Seeds two surveys:
		“Customer Satisfaction Survey” (with a conditional follow-up)
		“Employee Engagement Survey”
Each has one or more sample responses.
See SurveyTool.Infrastructure/Seed/SeedData.cs.

## Validation & errors
DTO validation with DataAnnotations + a Minimal API ValidationFilter<T> → 400 ValidationProblem for bad inputs.
Domain rule violations (e.g., “Question X is not visible”) surface as 400 ValidationProblem via UseExceptionHandler(...).
Unexpected errors return 500 Problem.

## Build & run commands
restore and build
	dotnet restore
	dotnet build

run https profile
	dotnet run --project SurveyTool.Api --launch-profile https

run http profile
	dotnet run --project SurveyTool.Api --launch-profile http

## Example: Submit a response
curl -X POST "https://localhost:7201/api/surveys/1/responses" ^
  -H "Content-Type: application/json" ^
  -d "{ \"items\": [ { \"questionId\": 1, \"selectedOptionIds\": [11] }, { \"questionId\": 3, \"freeText\": \"Great!\" } ] }"

Response
	{ "responseId": 301, "score": 5 }

## Assumptions
In-memory EF Core storage (no persistence across restarts).
Option weights are stable post-submission; stored scores reflect the rules at submission time.
FreeText answers are stored but do not affect the score.

## Project structure
SurveyTool.sln
SurveyTool/
	SurveyTool.Api/
	SurveyTool.Core/
	SurveyTool.Infrastructure/

See the [ERD diagram](docs/erd.md) for the data model.
