using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;

namespace Filterinator2000.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{

    public static string TextDump { get; set; } = string.Empty;

    private static double strictness = 0.4;
    public static double Strictness
    {
        get => Math.Round(strictness, 2); 
        set => strictness = Math.Round(value, 2);
    } 
    public static ObservableCollection<string> Results { get; set; } = new(TextDump.Split());
    
    
    public MainWindowViewModel()
    {
        
    }

}