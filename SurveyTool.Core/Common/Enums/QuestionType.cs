namespace SurveyTool.Core.Common.Enums
{
    /// <summary>
    /// Defines the types of questions supported in surveys.
    /// </summary>
    public enum QuestionType
    {
        /// <summary>
        /// User selects one answer option.
        /// </summary>
        SingleChoice = 0,

        /// <summary>
        /// User can select multiple answer options.
        /// </summary>
        MultipleChoice = 1,

        /// <summary>
        /// User provides free-text input.
        /// </summary>
        FreeText = 2
    }
}