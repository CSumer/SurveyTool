```mermaid
erDiagram
    Survey ||--o{ Question : has
    Question ||--o{ AnswerOption : has
    Survey ||--o{ SurveyResponse : receives
    SurveyResponse ||--o{ ResponseItem : contains
    Question }o--|| Question : parent

    Survey {
      int Id PK
      string Title
      string Description
    }

    Question {
      int Id PK
      int SurveyId FK
      string Text
      enum QuestionType
      int ParentQuestionId FK
      int[] VisibilityShowWhenAnyOptionIds
    }

    AnswerOption {
      int Id PK
      int QuestionId FK
      string Text
      int Weight
    }

    SurveyResponse {
      int Id PK
      int SurveyId FK
      datetime CreatedAt
      int Score
    }

    ResponseItem {
      int Id PK
      int SurveyResponseId FK
      int QuestionId
      string SelectedOptionIdsCsv
      string FreeText
    }