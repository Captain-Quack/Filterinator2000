using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Numerics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Microsoft.ML;
using Filterinator2000.Models;
using Filterinator2000.ViewModels;
using Microsoft.ML.Data;

namespace Filterinator2000.Views;

public partial class MainWindow
{
    private static StringBuilder _sb = new();

    private void Error(string console, string display)
    {
        sb.AppendLine(console);
        TextDump.Text = display;
        TextDump.Background = Brushes.Red;
    }

    private static StringBuilder sb = new(500); 
    private async void QbReaderRequest(string text)
    {
        sb.Clear(); 
        switch (text.Length)
        {
            case 0:
                return;
            case < 3:
                TextDump.Watermark = "Type at least three characters...";
                return;
            case > 1000: 
                Error("Text too long. Please shorten it.", "Text too long. Please shorten it.");
                return;
        }
        
        

        TextDump.Watermark = string.Empty;

        _searchButton.Content = _buttonContent.ring;
        var query = Uri.EscapeDataString(text);
        var Exact = EnabledMode.Contains("exact");
        var Similar = EnabledMode.Contains("Similar"); 
        if (Exact)
        {
            query = @$"^(?!.*[\(\{{\[][^)\}}\]]*\b{query}\b[^)\}}\]]*[\)\}}\]]).*\b{query}\b.*$" + "&regex=true";
        }
        else if (Similar)
        {
            query = $@"\b{query}\b" + "&regex=true";
        }

        
        var questionType = EnabledQuestionTypes.ToList() switch
        {
            ["tossup"] => "tossup",
            ["bonus"] => "bonus",
            _ => "all"
        };
        
        var searchType = EnabledQuestionParts.ToList() switch
        {
            ["question"] => "question",
            ["answer"] => "answer",
            _ => "all"
        };
        
        
        var args =
            $"?queryString={query}&questionType={questionType}&searchType={searchType}&exactPhrase={(Exact || Similar ? "false" : "true")}&difficulties={string.Join(',', EnabledDifficulties)}&categories={string.Join(',', EnabledCategories)}&subcategories={string.Join(',', EnabledSubCategories)}&alternateSubcategories={string.Join(',', EnabledAlternateSubCategories)}";
        sb.AppendLine("[Query]" + args + @"[\EndQuery]");
        using var response = await _client.GetAsync(args);

        CheckInternetSpeed(); 

        switch (response.StatusCode)
        {
            case HttpStatusCode.BadRequest:
                Error("Bad Request. This is because the query string is invalid.",
                    "Invalid query string. Something you searched was rejected for some reason.");
                return;

            case HttpStatusCode.RequestTimeout:
                Error("Request Timeout. This is because the server took too long to respond. Try again later.", """
                    Request Timeout. Try again later, possible reasons for error:
                    - You have a bad connection?
                    - QBReader is down?
                    - A lot of people are using QBReader at the same time?
                    """);
                return;
            case HttpStatusCode.Conflict:
                Error("Conflict. This is because the server is in a state that it cannot process the request.",
                    "Server conflict. Try again later or contact Geoffrey (geoffreywu1000@gmail.com)");
                return;
            case HttpStatusCode.RequestEntityTooLarge:
            case HttpStatusCode.RequestUriTooLong:
                Error("Request Entity Too Large. This is because the request is too large for the server to process.",
                    "Request too large. Don't paste the entire Bee Movie script into the search bar please.");
                return;
            case HttpStatusCode.UnprocessableEntity:
                Error(
                    "Unprocessable Entity. This is because the server cannot process the request due to semantic errors.",
                    "Unprocessable Entity. Try again later.");
                return;
            case HttpStatusCode.UpgradeRequired:
                Error(
                    "Upgrade Required. This is because the server cannot process the request because it requires an upgrade.",
                    "Upgrade Required. Contact Geoffrey please (geoffreywu1000@gmail.com)");
                break;
            case HttpStatusCode.TooManyRequests:
                Error(
                    "Too Many Requests. This is because the client has sent too many requests in a given amount of time.",
                    """
                    Too many requests. This can happen for two reasons:
                    - Too many people are using the QB Reader server at the same time. (this includes this app). 
                      QB Reader is capped at 20 requests per second, so this is likely the reason. Try again.
                    - You are spamming the search button. Please don't do that.
                    """);
                return;
            case HttpStatusCode.UnavailableForLegalReasons:
                Error("Unavailable For Legal Reasons. Woah!",
                    "Unavailable for legal reasons. Wherever you are, QB Reader is banned here (!)");
                return;
            case HttpStatusCode.InternalServerError:
                Error(
                    "Internal Server Error. This is because the server has encountered a situation it doesn't know how to handle.",
                    "Internal Server Error. Contact Geoffrey please (geoffreywu1000@gmail.com).");
                return;
            case HttpStatusCode.BadGateway:
                Error("Bad Gateway. This is because the server received an invalid response from an upstream server.",
                    """
                    Bad Gateway. This is likely not your fault, because either:
                    - QB Reader is down right now. Try again when it is up.
                    - QB Reader is overloaded. If you are also getting the "Too Many Requests" error, this 
                      might happen.
                    - QB Reader is misconfigured to handle requests. This shouldn't happen, 
                      but if you are getting this message at uncommon hours, contact Geoffrey please (geoffreywu1000@gmail.com).
                    In any case, try again. 
                    """);
                return;
            case HttpStatusCode.ServiceUnavailable:
                Error("Service Unavailable. This is because the server is down or overloaded, or the user is spamming things.",
                    """
                    >>> SERVICE UNAVAILABLE <<<
                    This is the most common error. This can happen for a few reasons:
                    - The user (you) is spamming the search button. Please don't do that.
                    - QB Reader is down. Try again later. 
                    - QB Reader is overloaded. Try again.
                    - Your internet is either (a) too slow or (b) preventing you from accessing QB Reader. Check your network settings.
                    If you would like to see if it is the app or QB Reader that is causing a problem
                    """); 
                return;
            case HttpStatusCode.GatewayTimeout:
                Error("Gateway Timeout",
                    """
                    Gateway timeout. This happens when QB Reader sends a response in a valid but unacceptably long time.
                    Possible Reasons:
                    - QB Reader is overloaded. Try again. 
                    - Your internet is either (a) too slow or (b) preventing you from accessing QB Reader. Check your network settings.
                    - (Unlikely) QB Reader is misconfigured. If you are getting this message at uncommon hours, contact Geoffrey please (geoffreywu1000@gmail.com), but try again first.
                    """);
                return;
            case HttpStatusCode.HttpVersionNotSupported:
                Error(
                    "HTTP Version Not Supported. This is because the server does not support the HTTP protocol version used in the request.",
                    "HTTP Version Not Supported. Contact Mason (masonlack26@usn.org).");
                return;
            case HttpStatusCode.InsufficientStorage:
                Error(
                    "Insufficient Storage. This is because the server has insufficient storage to complete the request.",
                    "Insufficient Storage. Contact Geoffrey please (geoffreywu1000@gmail.com)");
                return;
            case HttpStatusCode.NotExtended:
                Error(
                    "Not Extended. This is because the server does not support the HTTP protocol version used in the request.",
                    "Not Extended. Contact Mason (masonlack26@usn.org).");
                return;
            case HttpStatusCode.NetworkAuthenticationRequired:
                Error(
                    "Network Authentication Required. This is because the client needs to authenticate to gain network access.",
                    "Network Authentication Required. This shouldn't happen, so contact Geoffrey please (geoffreywu1000@gmail.com).");
                return;
            case HttpStatusCode.Continue:
            case HttpStatusCode.SwitchingProtocols:
            case HttpStatusCode.Processing:
            case HttpStatusCode.EarlyHints:
            case HttpStatusCode.OK:
            case HttpStatusCode.Created:
            case HttpStatusCode.Accepted:
            case HttpStatusCode.NonAuthoritativeInformation:
            case HttpStatusCode.NoContent:
            case HttpStatusCode.ResetContent:
            case HttpStatusCode.PartialContent:
            case HttpStatusCode.MultiStatus:
            case HttpStatusCode.AlreadyReported:
            case HttpStatusCode.IMUsed:
            case HttpStatusCode.Ambiguous:
            case HttpStatusCode.Moved:
            case HttpStatusCode.Found:
            case HttpStatusCode.RedirectMethod:
            case HttpStatusCode.NotModified:
            case HttpStatusCode.UseProxy:
            case HttpStatusCode.Unused:
            case HttpStatusCode.RedirectKeepVerb:
            case HttpStatusCode.PermanentRedirect:
            case HttpStatusCode.Unauthorized:
            case HttpStatusCode.PaymentRequired:
            case HttpStatusCode.Forbidden:
            case HttpStatusCode.NotFound:
            case HttpStatusCode.MethodNotAllowed:
            case HttpStatusCode.NotAcceptable:
            case HttpStatusCode.ProxyAuthenticationRequired:
            case HttpStatusCode.Gone:
            case HttpStatusCode.LengthRequired:
            case HttpStatusCode.PreconditionFailed:
            case HttpStatusCode.UnsupportedMediaType:
            case HttpStatusCode.RequestedRangeNotSatisfiable:
            case HttpStatusCode.ExpectationFailed:
            case HttpStatusCode.MisdirectedRequest:
            case HttpStatusCode.Locked:
            case HttpStatusCode.FailedDependency:
            case HttpStatusCode.PreconditionRequired:
            case HttpStatusCode.RequestHeaderFieldsTooLarge:
            case HttpStatusCode.NotImplemented:
            case HttpStatusCode.VariantAlsoNegotiates:
            case HttpStatusCode.LoopDetected:
            default:
                break;
        }

        response.EnsureSuccessStatusCode();
        

        TextDump.Background = Brushes.White;

        ProcessResult(await response.Content.ReadFromJsonAsync<QbReaderResponse>(new JsonSerializerOptions
        {
            AllowOutOfOrderMetadataProperties = true,
            AllowTrailingCommas = true,
            PropertyNameCaseInsensitive = true,
            TypeInfoResolver = new DefaultJsonTypeInfoResolver()
        }));

        _searchButton.Content = _buttonContent.search;

    }

    private void CheckInternetSpeed()
    {
        System.Net.NetworkInformation.Ping ping = new();
        var reply = ping.Send("www.qbreader.org");
        if (reply.Status != System.Net.NetworkInformation.IPStatus.Success)
        {
            Error("No Internet Connection", "No internet connection. Please check your network settings.");
        }
    }
private void ProcessResult(QbReaderResponse? readFromJsonAsync)
{
    if (readFromJsonAsync is null)
    {
        TextDump.Text = "No data provided.";
        return;
    }

    // Reset state and UI elements.
    _sb.Clear();
    TextDump.Text = string.Empty;
    MainWindowViewModel.Results.Clear();

    // Process Tossups (no filtering needed)
    if (readFromJsonAsync.Tossups?.Count > 0)
    {
        foreach (var tossup in readFromJsonAsync.Tossups.QuestionArray)
        {
            _sb.AppendLine(tossup.Question);
            _sb.AppendLine($"ANSWER: {tossup.Answer}\n");
        }
    }

    // Process Bonuses with filtering applied.
    if (readFromJsonAsync.Bonuses?.Count > 0)
    {
        // Zip bonus questions with answers.
        var bonusQuestions = readFromJsonAsync.Bonuses.QuestionArray.SelectMany(b => b.Questions);
        var bonusAnswers = readFromJsonAsync.Bonuses.QuestionArray.SelectMany(b => b.Answers);
        foreach (var (question, answer) in bonusQuestions.Zip(bonusAnswers, (q, a) => (q, a)))
        {
            
            if (!EnabledMode.Contains("explore") && !ShouldIncludeBonus(question, answer, _sBox.Text, EnabledQuestionParts))
            {
                continue;
            }
            _sb.AppendLine(question);
            _sb.AppendLine($"ANSWER: {answer}");
        }
    }

    // Clean and post-process the built text.
    var rawText = _sb.ToString();
    // First remove parenthesized text and fix known typos.
    var cleanedText = RemoveParenthases.Replace(rawText, string.Empty)
                                          .Replace(".name ", ". Name ");
    cleanedText = Spaces().Replace( RemovePeriodsFromAcronyms(cleanedText), " ");

    if (string.IsNullOrWhiteSpace(cleanedText))
    {
        TextDump.Text = "No results found. Try expanding your applied filters, or search for something else.";
        return;
    }

    TextDump.Text = cleanedText; 
    var content = RemoveAnswer().Replace(cleanedText, string.Empty); 
    var sentences = Sentence().Matches(content).AsParallel().Select(m => m.Value);
    var sentenceMatches = sentences
        .Select(s => s.Trim())
        .Where(s => !string.IsNullOrEmpty(s));

    // Remove similar sentences using ML.NET–based cosine similarity.
    var uniqueSentences = RemoveSimilar(sentenceMatches);
    uniqueSentences.ForEach(MainWindowViewModel.Results.Add);

    sb.AppendLine($"Unique sentence count: {MainWindowViewModel.Results.Count}");
    sb.AppendLine("[End]");
    Console.WriteLine(sb.ToString()); 
}

/// <summary>
/// Determines whether a bonus question/answer pair should be included based on the search filter and enabled parts.
/// </summary>
private bool ShouldIncludeBonus(string question, string answer, string filterText, IEnumerable<string> enabledParts)
{
    // Check if the filter text is contained in the question or answer.
    var containsQuestion = question.Contains(filterText, StringComparison.OrdinalIgnoreCase);
    var containsAnswer = answer.Contains(filterText, StringComparison.OrdinalIgnoreCase);
    var questionEnabled = enabledParts.Contains("question", StringComparer.OrdinalIgnoreCase);
    var answerEnabled = enabledParts.Contains("answer", StringComparer.OrdinalIgnoreCase);

    if (questionEnabled && !answerEnabled)
    {
        return containsQuestion;
    }
    if (answerEnabled && !questionEnabled)
    {
        return containsAnswer;
    }
    // When both (or neither) are enabled, require at least one match.
    return containsQuestion || containsAnswer;
}

private static readonly MLContext s_mlContext = new MLContext();

/// <summary>
/// Removes sentences that are too similar based on a combination of ML.NET cosine similarity
/// (with precomputed norms) and Jaccard similarity computed on significant words (after removing
/// non-significant qualifiers). The filtering is controlled by MainWindowViewModel.Strictness, where
/// 1.0 is lenient (includes nearly everything) and 0.0 is very strict.
/// </summary>
private static List<string> RemoveSimilar(IEnumerable<string> sentences)
{
    // --- Preprocessing ---
    // Remove quiz bowl qualifiers that are not significant.
    var qualifiersPattern = MyRegex();
    // Minimal stop-word list.
    var stopwords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "the", "a", "an", "and", "or", "but", "if", "is", "in", "at", "of", "on"
    };

    // Process each sentence once.
    var processedSentences = new List<(string Original, string Processed, HashSet<string> SignificantWords)>();
    foreach (var sentence in sentences)
    {
        // Remove qualifiers and extra spaces.
        var processed = qualifiersPattern.Replace(sentence, string.Empty);
        processed = Spaces().Replace(processed, " ").Trim();

        // Split into words and filter out short or unimportant words.
        var words = processed.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var significantWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var word in words)
        {
            if (word.Length > 2 && !stopwords.Contains(word))
            {
                significantWords.Add(word.ToLowerInvariant());
            }
        }

        // Only include sentences that have at least one significant word.
        if (significantWords.Count > 0)
        {
            processedSentences.Add((sentence, processed, significantWords));
        }
    }
    if (processedSentences.Count == 0)
    {
        return new List<string>();
    }

    // --- ML.NET Feature Extraction ---
    // Build feature vectors from the processed text.
    var data = processedSentences
        .Select(ps => new SentenceData { Text = ps.Processed })
        .ToList();
    var dataView = s_mlContext.Data.LoadFromEnumerable(data);
    var pipeline = s_mlContext.Transforms.Text.FeaturizeText(
        outputColumnName: "Features",
        inputColumnName: nameof(SentenceData.Text));
    var model = pipeline.Fit(dataView);
    var transformedData = model.Transform(dataView);
    var featureColumn = transformedData.GetColumn<float[]>("Features").ToArray();

    // --- Precompute Norms ---
    // Compute Euclidean norms for each feature vector.
    int n = featureColumn.Length;


    // --- Similarity Filtering ---
    // Use MainWindowViewModel.Strictness as a similarity threshold.
    var similarityThreshold = MainWindowViewModel.Strictness;
    var representatives = new List<int>(); // Holds indices of unique sentences.

    // For each sentence, compare it against previously accepted representatives.
    for (int i = 0; i < n; i++)
    {
        bool isDuplicate = false;
        for (int k = 0; k < representatives.Count; k++)
        {
            int repIdx = representatives[k];
            float cosine = CosineSimilarity(featureColumn[i], featureColumn[repIdx]);
            float jaccard = JaccardSimilarity(processedSentences[i].SignificantWords, processedSentences[repIdx].SignificantWords);
            float combinedSimilarity = 0.7f * cosine + 0.3f * jaccard;
            if (combinedSimilarity >= similarityThreshold)
            {
                isDuplicate = true;
                break;
            }
        }
        if (!isDuplicate)
        {
            representatives.Add(i);
        }
    }

    // --- Build and Return Result ---
    var result = new List<string>(representatives.Count);
    foreach (int idx in representatives)
    {
        result.Add(processedSentences[idx].Original);
    }
    sb.AppendLine($"RemoveSimilar returning {result.Count} unique sentences after optimization.");
    return result;
}



/// <summary>
/// Computes the Jaccard similarity between two sets of strings.
/// </summary>
private static float JaccardSimilarity(HashSet<string> setA, HashSet<string> setB)
{
    if (setA.Count == 0 || setB.Count == 0)
        return 0f;
    var intersectionCount = setA.Intersect(setB).Count();
    var unionCount = setA.Union(setB).Count();
    return (float)intersectionCount / unionCount;
}

    
    public static string RemovePeriodsFromAcronyms(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        // Regex pattern to identify individual letters with periods in acronyms

        // Use Regex.Replace with a MatchEvaluator to remove periods from the matched acronym parts
        var result = RemovePeriodAcronym().Replace(input, match => match.Groups[1].Value);

        
        return result;
    }
    
    
    private static readonly Regex RemoveParenthases = RemoveExtra();
    private static string _searchType = "all";
    private static string _questionType = "all";
    private static bool? _exact = null; 
    const string pendanticTemplate = @"(^[^A-z]*\b{0}\b(?=.*\[))|(\b{0}\b(?=.*for 10 points))|(For 10 points.+{0}\b)";


    [GeneratedRegex(@"(\(.+?\))|(\[.+?\])|( ?for 10 points( |, )?)", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
    private static partial Regex RemoveExtra();
    [GeneratedRegex("""(?:“|")?\[?(T(?=(his|hese)))?(?(1)(?:his|hese)\b\s(?:[^.“"]+(“|")?(?(3)[^"”]+))+?(?:\s[A-Z](\.|\?)|[^.?])*(\.|\?)|((?:(?:[^“".?]+\b(this|these|here)\b[^“".?]+(\.|\?))((“|")(?=[^"”]*(”|")(?:\w|\s)*(\.|\?))[^”".?]+(”|"")[^?.]+(\.|\?))?)|(?:(?:[\w\s,]+(“|")(?=.*\[(this|these))(?=[^”“]*[”"](?:\w|\s)*\.)[^”“"?.]+(”|"))[^?.]*?(?:\b(this|these|here)\b)?[^?.]*(\.|\?)|(?:\s[A-Z]\.|[^?.])*\b(what|this|these|here|[iI]t'?s?)\b[^.?]*(\.|\?))))(?:"|”)?""", RegexOptions.Compiled)]
    private static partial Regex Sentence();
    [GeneratedRegex(@"(?<!\w)([A-Z])\.(?=([A-Z]\.)*(\s|\)|$))")]
    private static partial Regex RemovePeriodAcronym();
    [GeneratedRegex(@" (\[|\().*")]
    private static partial Regex PureAnswer();
    
    [GeneratedRegex(@"^ANSWER:.*$", RegexOptions.Compiled | RegexOptions.Multiline)]
    private static partial Regex RemoveAnswer();
    [GeneratedRegex(@" {2,}")]
    private static partial Regex Spaces();
    [GeneratedRegex(@"\b(this|these|where|here)\b", RegexOptions.IgnoreCase, "en-US")]
    private static partial Regex MyRegex();
}