using System.ComponentModel.DataAnnotations;

namespace SurveyTool.Api.Infrastructure.Validation
{
    public sealed class ValidationFilter<T> : IEndpointFilter where T : class
    {
        public async ValueTask<object?> InvokeAsync(
            EndpointFilterInvocationContext context,
            EndpointFilterDelegate next)
        {
            var dto = context.Arguments.OfType<T>().FirstOrDefault();
            if (dto is null)
            {
                // Nothing to validate for this endpoint; continue
                return await next(context);
            }

            var results = new List<ValidationResult>();
            var valid = Validator.TryValidateObject(
                dto,
                new ValidationContext(dto),
                results,
                validateAllProperties: true);

            if (valid)
            {
                return await next(context);
            }

            // Build ProblemDetails-style error dictionary: { "Field": ["msg1","msg2"], "" : ["msg"] }
            var errors = results
                .SelectMany(r =>
                    (r.MemberNames?.Any() == true ? r.MemberNames : new[] { string.Empty })
                    .Select(member => new { member, message = r.ErrorMessage ?? "Invalid" }))
                .GroupBy(x => x.member)
                .ToDictionary(g => g.Key, g => g.Select(x => x.message).ToArray());

            return Results.ValidationProblem(errors);
        }
    }
}