using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using System.IO;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using FluentAvalonia.UI.Controls;


namespace Filterinator2000.Views;

using Options = HashSet<string>; 

public sealed partial class MainWindow : Window
{

    private readonly HttpClient _client = new() { BaseAddress = new Uri("https://www.qbreader.org/api/query") };
    private Button _searchButton;
    private readonly (object search, ProgressRing? ring) _buttonContent = (null, null)!;
    private readonly TextBox _textBox;
    private readonly AutoCompleteBox _sBox;
    private static readonly HashSet<MenuItem> Modes = []; 
    private static readonly Options EnabledDifficulties = [];
    private static readonly Options EnabledCategories = [];
    private static readonly Options EnabledSubCategories = [];
    private static readonly Options EnabledAlternateSubCategories = []; 
    private static readonly Options EnabledQuestionTypes = [];
    private static readonly Options EnabledQuestionParts = [];
    private static readonly Options EnabledMode = [];



    private readonly System.Timers.Timer _timer = new()
    {
        AutoReset = false,
        Interval = 500,
        Enabled = false
    };



    public MainWindow()
    {
        InitializeComponent();
        _sBox = this.FindControl<AutoCompleteBox>("SBox")!;
        _textBox = this.FindControl<TextBox>("TextDump")!;
        _searchButton = this.FindControl<Button>("SearchButton")!;
        // these next two lines are due to a dumb thing where the flyout of a menuitem are pains in butts to fetch.
        Modes.UnionWith([this.FindControl<MenuItem>("exact")!, this.FindControl<MenuItem>("similar")!, this.FindControl<MenuItem>("explore")!]);
        ToggleOn(Modes.ElementAt(0), EnabledMode, "exact");
        
        _buttonContent.search = _searchButton.Content!;
        // make sure image exists
        _buttonContent.ring = new ProgressRing()
        {
            IsIndeterminate = true,
            BorderBrush = Brushes.Black,
            BorderThickness = new Thickness(3)

        };
        

        _timer.Elapsed += (_, _) => { Dispatcher.UIThread.Post(() => { QbReaderRequest(_sBox.Text!); }); };
        var sBoxItemsSource = File.ReadAllLines("Assets/qb_items.txt")
            .Distinct()
            .Where(x => x.Length < 30)
            .OrderBy(s => Random.Shared.Next(100))
            .ThenBy(s => s.Split().Length)
            .ThenBy(s => s.Length).ToList();
        _sBox.ItemsSource = sBoxItemsSource;

        _sBox.TextFilter = (search, item) =>
        {
            if (item is null || search is null) return false;

            return item.Contains(search, StringComparison.OrdinalIgnoreCase)
                   || string.Concat(item.Split().Where(s => !string.IsNullOrEmpty(s)).Select(s => s[0]))
                       .Contains(search, StringComparison.OrdinalIgnoreCase);

        };
        
        QuestionOrAnswer_OnChecked(this.FindControl<MenuItem>("answer"), new RoutedEventArgs());

        

        TextDump.Watermark = """
                             *** README ***
                             Filterinator2000:
                             -  Uses algorithm (controlled with slider) to filter out similar clues. Try playing around with it! Set it to, e.g., 0.2.
                             - Searches QB Reader API for quizbowl questions.
                             - Choose your Category (e.g., Literature or Science), 
                             - Pick a Difficulty (same system as QB Reader)
                             - Toggle between tossups / bonuses, questions / answers (answer only is default)
                             - Fine-tune precision (Exact, Similar, Explore). Explore searches exactly what you searched. Similar implements checks to remove off topic results.  Exact uses Mason's overengineered "regular" expression to get rid of more annoying clues.
                             - Finally, type something in the search box and hit enter or click the search button.
                             - Random Search Suggestion: 
                             """
                             + sBoxItemsSource[Random.Shared.Next(sBoxItemsSource.Count)];


    }

    private void Search(object? sender, RoutedEventArgs e)
    {
        if (sender is not (Button or AutoCompleteBox)) return;
        QbReaderRequest(_sBox.Text!);
    }

    private CancellationTokenSource? _typingCts;

    private void SBox_OnTextChanged(object? sender, TextChangedEventArgs e)
    {
        _typingCts?.CancelAsync();
        _typingCts = new();
        _timer.Stop();
        _timer.Start();
    }

    private void SBox_OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (sender is not AutoCompleteBox box) return;
        switch (e.Key)
        {
            case Key.Space:
                _typingCts?.CancelAsync();
                _typingCts = new();
                _timer.Stop();
                _timer.Start();


                break;
            case Key.Enter:
                _typingCts?.CancelAsync();
                Search(box, e);
                break;
            case Key.Escape:
                _textBox.Focus();
                break;
        }
    }
    
    private void TossupsOrBonuses_OnChecked(object? sender, RoutedEventArgs e)
    {
        if(sender is not MenuItem { Name: { } name, Tag: bool wasSelected } tossupOrBonus) return;
        ((Action<MenuItem, Options, string>) (wasSelected ? ToggleOff : ToggleOn))(tossupOrBonus, EnabledQuestionTypes, name);
        _timer.Stop();
        _timer.Start();
    }
    
    private void QuestionOrAnswer_OnChecked(object? sender, RoutedEventArgs e)
    {
        if(sender is not MenuItem { Name: string name, Tag: bool wasSelected } questionOrAnswer) return;
        ((Action<MenuItem, Options, string>) (wasSelected ? ToggleOff : ToggleOn))(questionOrAnswer, EnabledQuestionParts, name);
        _timer.Stop();
        _timer.Start();
    }
    
    private void Strictness_OnChecked(object? sender, RoutedEventArgs e)
    {
        if(sender is not MenuItem { Name: string name, Tag: bool wasSelected } mode) return;
        
        // this one is a bit different, we want to remove all other modes if one is selected. 
        
        if (wasSelected) return; 
        ToggleOn(mode, EnabledMode, name);
        foreach (var item in Modes.Where(item => item != mode))
        {
            ToggleOff(item, EnabledMode, (item.Header as string)!.ToLower());
        }
        
        _timer.Stop();
        _timer.Start();
        
    }
    
    
    
    


    private void ToggleButton_OnIsCheckedChanged(object? o, RoutedEventArgs e)
    {
        if (o is not MenuItem { Tag: bool wasSelected, Name: { } diffName } diff) return;
        Console.WriteLine(diffName[1..]);
        ((Action<MenuItem, Options, string>) (wasSelected ? ToggleOff : ToggleOn))(diff, EnabledDifficulties, diffName[1..]);
        _timer.Stop();
        _timer.Start();
    }
    

   
    
    public void ToggleCategoryFilter(object? sender, PointerPressedEventArgs e)
    {
        
        if (sender is not MenuItem { Tag : bool wasSelected } category || category.Items.Any( x => ((MenuItem)x!).IsSelected)) return;
        ((Action<MenuItem>)(wasSelected ? Remove : Enable))(category);
        ((Action<MenuItem>)(wasSelected ? _ => { } : EnableRoot))(category);
        
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void EnableRoot(MenuItem category)
    {
        if (category.Parent is not MenuItem parent) return;
        
        ToggleOn(category, CategorySwitcher(category), (category.Header as string)!);
        EnableRoot(parent);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Remove(MenuItem category)
    {        
        ToggleOff(category, CategorySwitcher(category), (category.Header as string)!);
        
        foreach (MenuItem child in category.Items)
        {
            Remove(child);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Enable(MenuItem category)
    {
        ToggleOn(category, CategorySwitcher(category), (category.Header as string)!);

        foreach (MenuItem child in category.Items)
        {
            Enable(child);
        }
    }

    Options _dummy = [];
    private Options CategorySwitcher(MenuItem category)
    {
        
        return category.Header switch
        {
            "Literature" or "History" or "Science" or "Fine Arts" or "Religion" or "Mythology" or "Philosophy" or "Social Science" or "Current Events" or "Geography" or "Other Academic" or "Trash" => EnabledCategories,
            "American Literature" or  "British Literature" or  "Classical Literature" or  "European Literature" or  "World Literature" or  "Other Literature" or  "American History" or  "Ancient History" or  "European History" or  "World History" or  "Other History" or  "Biology" or  "Chemistry" or  "Physics" or  "Other Science" or  "Visual Fine Arts" or  "Auditory Fine Arts" or  "Other Fine Arts" => EnabledSubCategories, 
            "Drama" or  "Long Fiction" or  "Poetry" or  "Short Fiction" or  "Misc Literature" or  "Math" or  "Astronomy" or  "Computer Science" or  "Earth Science" or  "Engineering" or  "Misc Science" or  "Architecture" or  "Dance" or  "Film" or  "Jazz" or  "Opera" or  "Photography" or  "Misc Arts" or  "Anthropology" or  "Economics" or  "Linguistics" or  "Psychology" or  "Sociology" or "Other Social Science" => EnabledAlternateSubCategories,
            "Close" or "Toggle All" or "-" or "Alternate" => _dummy, 
            _ => throw new ArgumentOutOfRangeException("Unknown Category: " + category.Header),
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ToggleOff(MenuItem item, Options set, string remove)
    {
        var header = item.Header as string; 
        if(header is "Close" or "Toggle All" or "-") return;
        set.Remove(remove);
        item.Background = Brushes.Transparent;
        item.Foreground = Brushes.Black;
        item.Tag = false;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ToggleOn(MenuItem category, Options set, string remove)
    {
        var header = category.Header as string; 
        if(header is "Close" or "Toggle All" or "-") return;
        set.Add(remove);
        category.Background = Brushes.LightBlue;
        category.Foreground = Brushes.White;
        category.Tag = true;
    }

    private void ToggleAllCategories(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not MenuItem { Tag: bool wasSelected } category) return;
        Action<MenuItem> action = wasSelected ? Remove : Enable;
        foreach (MenuItem item in (category.Parent as MenuFlyoutPresenter).Items)
        {   
            
            action(item!);
        }
        category.Tag = !wasSelected;
    }
    
    private void ToggleAllDifficulties(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not MenuItem { Tag: bool wasSelected } category) return;
        Action<MenuItem, Options, string> action = wasSelected ? ToggleOff : ToggleOn;
        foreach (MenuItem item in (category.Parent as MenuFlyoutPresenter).Items)
        {   
            action(item!, EnabledDifficulties, (item.Header as string)![1..]);
        }
        category.Tag = !wasSelected;
    }
    
    private async void OnCopyItem(object? sender, RoutedEventArgs e)
    {
        if (sender is MenuItem menuItem && menuItem.Tag is string itemText)
        {
            await Clipboard!.SetTextAsync(itemText);
        }
    }

    private async void OnPasteItem(object? sender, RoutedEventArgs e)
    {
        if (sender is not MenuItem { Tag: string originalText }) return;
        // Get text from clipboard
        var pastedText = await Clipboard!.GetTextAsync();
                
        // Example: just show the pasted text in a messagebox, or handle it however you like
        var msgBox = new Window
        {
            Width = 300,
            Height = 150,
            Content = new TextBlock
            {
                Text = $"Original item: {originalText}\nPasted text: {pastedText}",
                TextWrapping = Avalonia.Media.TextWrapping.Wrap
            }
        };
        await msgBox.ShowDialog(this);
    }

    private async void SearchResultMenu_OnPressed(object sender, PointerPressedEventArgs e)
    {
        if (!ReferenceEquals(sender, e.Source))
        {
            return;
        }

        e.Handled = true;

        TextBlock tb = null;
        string originalText = string.Empty;

        if (sender is TextBlock textBlock)
        {
            tb = textBlock;
            originalText = textBlock.Text;
        }
        else if (sender is Border border)
        {
            tb = border.FindControl<TextBlock>("block");
            if (tb != null)
                originalText = tb.Text;
        }

        // Copy to clipboard
        await Clipboard!.SetTextAsync(originalText);

        if (tb != null)
        {
            tb.Text = "Copied!";
            await Task.Delay(1000);
            tb.Text = originalText;
        }
    }
}

