using System.Windows;

namespace ImageProcessor.Views;

/// <summary>
/// Code-behind главного окна.
/// Вся логика вынесена в <see cref="ViewModels.MainViewModel"/> — здесь только инициализация.
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }
}
