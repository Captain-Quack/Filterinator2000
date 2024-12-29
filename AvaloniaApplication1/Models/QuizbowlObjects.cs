using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Tmds.DBus.Protocol;

namespace Filterinator2000.Models;

public sealed record QbReaderResponse
{
    public TossupResults? Tossups { get; init;  }
    public BonusResults? Bonuses { get; init; }
    public required string QueryString { get; init; }
}

public sealed record TossupResults
{
    public required int Count { get; init; }
    public required List<Tossup> QuestionArray { get; init; }
}

public sealed record Tossup
{
    [JsonPropertyName("question_sanitized")]
    public required string Question { get; set; }
    [JsonPropertyName("answer_sanitized")]
    public required string Answer { get; set; }
    [JsonPropertyName("category")]
    public required string Category { get; init; }
    [JsonPropertyName("subcategory")]
    public required string Subcategory { get; init; }
    [JsonPropertyName("alternate_subcategory")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? AlternateSubcategory { get; init; }
    [JsonPropertyName("difficulty")]
    public required int Difficulty { get; init; }
}

public sealed record BonusResults
{
    public required int Count { get; init; }
    public required List<Bonus> QuestionArray { get; init; }
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
}
