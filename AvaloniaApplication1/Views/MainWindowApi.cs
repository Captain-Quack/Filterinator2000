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
using Avalonia.Media;
using Microsoft.ML;
using Filterinator2000.Models;
using Filterinator2000.ViewModels;
using Microsoft.ML.Data;

namespace Filterinator2000.Views;

public partial class MainWindow
{
    private StringBuilder _sb = new();

    private static string QuestionType
    {
        get => _questionType;
        set => _questionType = EnsureAcceptable(value, ["all", "tossup", "bonus"]);
    }

    private static string SearchType
    {
        get => _searchType;
        set => _searchType = EnsureAcceptable(value, ["all", "question", "answer"]);
    } 
    
    private static bool? Exact
    {
        get => _exact;
        set => _exact = value;
    }



    private void Error(string console, string display)
    {
        Console.Error.WriteLine(console);
        TextDump.Text = display;
        TextDump.Background = Brushes.Red;
    }

    private async void QbReaderRequest(string text)
    {
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

        _searchButton.Content = buttonContent.ring;
        var query = Uri.EscapeDataString(text);
        if (Exact == true)
        {
            query = string.Format(pendanticTemplate, Regex.Escape(query)) + "&regex=true"; 
        }
        var args =
            $"?queryString={query}&questionType={QuestionType}&searchType={SearchType}&exactPhrase={(Exact ?? true ? "true" : "false")}&difficulties={string.Join(',', EnabledDifficulties)}";
        Console.WriteLine("Querying: " + args);
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
                    Service Unavailable. This error happens for several reasons:
                    - (Most Likely) You need to slow down. You are sending too many requests to QB Reader at once.
                    - (Possible) High traffic for QB Reader (wait a bit)
                    - (Rare) Scheduled Maintenance
                    - (Improbable) QB Reader is misconfigured. If you are getting this message at uncommon hours,
                      but the server is up, contact Geoffrey please (geoffreywu1000@gmail).
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

        var result = ProcessResult(await response.Content.ReadFromJsonAsync<QbReaderResponse>(new JsonSerializerOptions
        {
            AllowOutOfOrderMetadataProperties = true,
            AllowTrailingCommas = true,
            PropertyNameCaseInsensitive = true,
            TypeInfoResolver = new DefaultJsonTypeInfoResolver()
        }));

        _searchButton.Content = buttonContent.image;

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

    private QbReaderResponse ProcessResult(QbReaderResponse? readFromJsonAsync)
    {
        _sb.Clear(); 
        TextDump.Text = string.Empty;
        MainWindowViewModel.Results.Clear();


        // Dictionary<string, string> clueToAnswer =
        //    new((int)BitOperations.RoundUpToPowerOf2((uint)(readFromJsonAsync!.Tossups!.Count + readFromJsonAsync.Bonuses!.Count * 10))); 
        
        
        if (readFromJsonAsync!.Tossups!.Count > 0){

                foreach (var (question, answer) in readFromJsonAsync.Tossups!.QuestionArray.AsParallel().Select(t => (t.Question, t.Answer)))
                {
                    
                    _sb.AppendLine(question);
                    _sb.AppendLine("ANSWER: " + answer + "\n"); 
                }
        }
        

        _sb.AppendLine();

        if (readFromJsonAsync.Bonuses!.Count > 0)
        {
            foreach (var (question, answer) in readFromJsonAsync.Bonuses!.QuestionArray.AsParallel().SelectMany(b => b.Questions).Zip(readFromJsonAsync.Bonuses!.QuestionArray.AsParallel().SelectMany(b => b.Answers)))
            {
                _sb.AppendLine(question);
                _sb.AppendLine("ANSWER: " + answer);
                
                }
        }

        var text = RemovePeriodsFromAcronyms(RemoveParenthases.Replace(_sb.ToString(), string.Empty).Replace(".name ", ". Name "));

        if (string.IsNullOrWhiteSpace(text)) return readFromJsonAsync;
        TextDump.Text = text;


        var sentences = Sentence().Matches(RemoveAnswer().Replace(text, string.Empty)).AsParallel()
            .Select(m => m.Value.Trim());

        RemoveSimilar(sentences).ForEach(MainWindowViewModel.Results.Add);
        
        Console.WriteLine(MainWindowViewModel.Results.Count);


        return readFromJsonAsync;
    }

    private List<string> RemoveSimilar(ParallelQuery<string> sentences)
    {
        
        var mlContext = new MLContext();
        var data = sentences.Select(s => new SentenceData { Text = s }).ToList();
        IDataView dataView = mlContext.Data.LoadFromEnumerable(data);

        var pipeline = mlContext.Transforms.Text.FeaturizeText(
            outputColumnName: "Features",
            inputColumnName: nameof(SentenceData.Text));

        var model = pipeline.Fit(dataView);
        var transformedData = model.Transform(dataView);
        var featureColumn = transformedData
            .GetColumn<float[]>("Features")
            .ToArray();
        var representatives = new List<int>();
        for (int i = 0; i < featureColumn.Length; i++)
        {
            bool isDuplicate = false;
            foreach (int repIdx in representatives)
            {
                float sim = CosineSimilarity(featureColumn[i], featureColumn[repIdx]);
                if (sim >= 0.8f) // threshold can be tuned
                {
                    isDuplicate = true;
                    break;
                }
            }
            if (!isDuplicate)
                representatives.Add(i);
        }

        return representatives.Select(i => data[i].Text).ToList(); 




    }
    private static float CosineSimilarity(float[] vecA, float[] vecB)
    {
        if (vecA.Length != vecB.Length)
            throw new ArgumentException("Vectors must be the same length.");

        float dot = 0f;
        float normA = 0f;
        float normB = 0f;

        for (var i = 0; i < vecA.Length; i++)
        {
            dot += vecA[i] * vecB[i];
            normA += vecA[i] * vecA[i];
            normB += vecB[i] * vecB[i];
        }

        return (float)(dot / (Math.Sqrt(normA) * Math.Sqrt(normB)));
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
    const string pendanticTemplate = @"\b{0}\b(?=[\[(])|^(?!.*[\[(]).*\b{0}\b";


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
}
