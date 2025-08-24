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
      string? Description
    }

    Question {
      int Id PK
      int SurveyId FK
      string Text
      enum QuestionType  // SingleChoice, MultipleChoice, FreeText
      int? ParentQuestionId FK  // conditional parent
      int[]? VisibilityShowWhenAnyOptionIds  // triggers
    }

    AnswerOption {
      int Id PK
      int QuestionId FK
      string Text
      int Weight  // optional effect if 0
    }

    SurveyResponse {
      int Id PK
      int SurveyId FK
      datetime CreatedAt
      int Score  // persisted total at submit time
    }

    ResponseItem {
      int Id PK
      int SurveyResponseId FK
      int QuestionId
      string? SelectedOptionIdsCsv  // e.g. "10,11" (null for FreeText)
      string? FreeText
    }
