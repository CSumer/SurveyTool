using SurveyTool.Api.Contracts.Options;

namespace SurveyTool.Api.Contracts.Questions;

public record QuestionDto(
        int Id, string Text, QuestionType Type, int? ParentQuestionId,
        IEnumerable<int>? ShowWhenAnyOptionSelected, IEnumerable<AnswerOptionDto> Options);