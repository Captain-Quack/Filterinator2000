using System;
using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using FluentAvalonia.Core;
using FluentAvalonia.UI.Controls;


namespace Filterinator2000.Views;

public partial class MainWindow : Window
{

    private readonly TextBox _textBox;
    private readonly AutoCompleteBox _sBox;
    
    

    private readonly System.Timers.Timer _timer = new()
    {
        AutoReset = false,
        Interval = 500,
        Enabled = false
    };

    private readonly HttpClient _client = new() { BaseAddress = new Uri("https://www.qbreader.org/api/query")  };
    
    
    private Button _searchButton;  
    private (Avalonia.Svg.Skia.Svg? image, ProgressRing? ring) buttonContent = (null, null);
    
    public MainWindow()
    {
        InitializeComponent();
        _sBox = this.FindControl<AutoCompleteBox>("SBox")!;
        _textBox = this.FindControl<TextBox>("TextDump")!;
        _searchButton = this.FindControl<Button>("SearchButton")!; 
        Difficulties = this.FindControl<ComboBox>("Difficulties")!;
        QuestionType = "all";
        SearchType = "all"; 
        buttonContent.image = (Avalonia.Svg.Skia.Svg)_searchButton.Content!; 
        buttonContent.ring = new ProgressRing()
        {
            IsIndeterminate = true,
            BorderBrush = Brushes.Black,
            BorderThickness = new(3)

        };

        // now hook the event after everything is loaded
        Difficulties.SelectionChanged += Difficulties_OnSelectionChanged;
        
        
        
        _timer.Elapsed += (_, _) =>
        {    Dispatcher.UIThread.Post(() => 
            {
                QbReaderRequest(_sBox.Text!);
            });
        };
        var sBoxItemsSource = File.ReadAllLines("Assets\\qb_items.txt").Distinct().OrderBy(s => Random.Shared.Next(100)).ThenBy(s => s.Split().Length)
            .ThenBy(s => s.Length).ToList();
        _sBox.ItemsSource = sBoxItemsSource;

        _sBox.TextFilter = (search, item) =>
        {
            if (item is null || search is null) return false;

            return item.Contains(search, StringComparison.OrdinalIgnoreCase)
                   || string.Concat(item.Split().Where(s => !string.IsNullOrEmpty(s)).Select(s => s[0]))
                       .Contains(search, StringComparison.OrdinalIgnoreCase);

        };
        
        TextDump.Watermark = "Search for something. Random suggestion: "
                             + sBoxItemsSource[Random.Shared.Next(sBoxItemsSource.Count)];


    }


    private static string EnsureAcceptable(string test, ReadOnlySpan<string> acceptable)
    {
        if (!acceptable.Contains(test))
        {
            throw new ArgumentException($"Unacceptable input: {test}"); 
        }

        return test; 
    }
    
    private void Search(object? sender, RoutedEventArgs e)
    {
        if (sender is not (Button or AutoCompleteBox)) return;
        QbReaderRequest(_sBox.Text!); 
    }

    private CancellationTokenSource? _typingCts;

    private async void SBox_OnTextChanged(object? sender, TextChangedEventArgs e)
    {
        _typingCts?.CancelAsync();
        _typingCts = new(); 
        _timer.Stop();
        _timer.Start();
    }

    private async void SBox_OnKeyDown(object? sender, KeyEventArgs e)
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

    private void SettingChanged(object? sender, RoutedEventArgs e)
    {

        switch (sender)
        {
            case CheckBox { Name: "TossupOrBonus", IsThreeState: true, IsChecked: var c1 } tb:
                (QuestionType, tb.Content) = c1 switch
                {
                    true => ("all", "Tossups & Bonuses"),
                    false => ("bonus", "Bonuses Only"),
                    null => ("tossup", "Tossups Only")
                };
                break;
            case CheckBox { Name: "QuestionOrAnswer", IsThreeState: true, IsChecked: var c2 } qa:
                (SearchType, qa.Content) = c2 switch
                {
                    true => ("all", "Questions & Answers"),
                    false => ("answer", "Answers Only (Work in Progress)"),
                    null => ("question", "Questions Only")
                };
                break;
            case CheckBox { Name: "ExactPhrase", IsThreeState: true, IsChecked: var c3 } ex:
                (Exact, ex.Content) = c3 switch
                {
                    true => (true, "Pedantic (Work in Progress)"),
                    null => (null as bool?, "Similar"), 
                    false => (false, "Explore")
                }; 
                break;
        }

        QbReaderRequest(_sBox.Text!);
    }


    private static readonly HashSet<string>[] Sets = [["0"], ["1"], ["2"], ["3"], ["4"], ["5"], ["6"], ["7"], ["8"], ["9"], ["10"]];

    private static readonly HashSet<string> EnabledDifficulties = [];   
     private void ToggleButton_OnIsCheckedChanged(object? o, RoutedEventArgs e)
     {

         if (o is not CheckBox b) return; 
         
         
         EnabledDifficulties.SymmetricExceptWith(Sets[int.Parse((string)b.Tag!)]);
         
     }  

     private void Difficulties_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
     {

         if (sender is not ComboBox cb) return;

         cb.SelectedIndex = 0;
     }

     private void Difficulties_OnDropDownClosed(object? sender, EventArgs e)
     {
         Console.WriteLine("Difficulties: {0}", string.Join(", ", EnabledDifficulties));
         QbReaderRequest(_sBox.Text!);
     }
}