using Playground.Core.ViewModels;
using Xamarin.Forms;

namespace Playground.Core.Pages
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
            BindingContext = new MainViewModel();
        }
    }
}