using Lab2.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace Lab2
{
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
            this.DataContext = new MainViewModel(this);
        }
    }
}