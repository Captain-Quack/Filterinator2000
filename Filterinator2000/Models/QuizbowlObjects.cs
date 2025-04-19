using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Filterinator2000.Models;

public sealed record QbReaderResponse
{
    public TossupResults? Tossups { get; init; }
    public BonusResults? Bonuses { get; init; }
    public required string QueryString { get; init; }

    [JsonConstructor]
    public QbReaderResponse(TossupResults? tossups, BonusResults? bonuses, string queryString)
    {
        Tossups = tossups;
        Bonuses = bonuses;
        QueryString = queryString;
    }
}

public sealed record TossupResults
{
    public required int Count { get; init; }
    public required List<Tossup> QuestionArray { get; init; }

    [JsonConstructor]
    public TossupResults(int count, List<Tossup> questionArray)
    {
        Count = count;
        QuestionArray = questionArray;
    }
}

public sealed record Tossup
{
    [JsonPropertyName("question_sanitized")]
    public required string Question { get; init; }

    [JsonPropertyName("answer_sanitized")]
    public required string Answer { get; init; }

    [JsonPropertyName("category")]
    public required string Category { get; init; }

    [JsonPropertyName("subcategory")]
    public required string Subcategory { get; init; }

    [JsonPropertyName("alternate_subcategory")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? AlternateSubcategory { get; init; }

    [JsonPropertyName("difficulty")]
    public required int Difficulty { get; init; }

    [JsonConstructor]
    public Tossup(string question, string answer, string category, string subcategory, string? alternateSubcategory, int difficulty)
    {
        Question = question;
        Answer = answer;
        Category = category;
        Subcategory = subcategory;
        AlternateSubcategory = alternateSubcategory;
        Difficulty = difficulty;
    }
}

public sealed record BonusResults
{
    public required int Count { get; init; }
    public required List<Bonus> QuestionArray { get; init; }

    [JsonConstructor]
    public BonusResults(int count, List<Bonus> questionArray)
    {
        Count = count;
        QuestionArray = questionArray;
    }
}

public sealed record Bonus
{
    [JsonPropertyName("parts_sanitized")]
    public required string[] Questions { get; init; }

    [JsonPropertyName("answers_sanitized")]
    public required string[] Answers { get; init; }

    [JsonPropertyName("category")]
    public required string Category { get; init; }

    [JsonPropertyName("subcategory")]
    public required string Subcategory { get; init; }

    [JsonPropertyName("alternate_subcategory")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? AlternateSubcategory { get; init; }

    [JsonPropertyName("difficulty")]
    public required int Difficulty { get; init; }

    [JsonConstructor]
    public Bonus(string[] questions, string[] answers, string category, string subcategory, string? alternateSubcategory, int difficulty)
    {
        Questions = questions;
        Answers = answers;
        Category = category;
        Subcategory = subcategory;
        AlternateSubcategory = alternateSubcategory;
        Difficulty = difficulty;
    }
}