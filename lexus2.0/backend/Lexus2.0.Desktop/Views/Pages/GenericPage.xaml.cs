using System.Windows;
using System.Windows.Controls;

namespace Lexus2_0.Desktop.Views.Pages
{
    public partial class GenericPage : UserControl
    {
        public GenericPage()
        {
            InitializeComponent();
        }

        public GenericPage(string title, string description, string category) : this()
        {
            TitleTextBlock.Text = title;
            DescriptionTextBlock.Text = description;
        }

        private void ConfigureButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show($"Configuration for '{TitleTextBlock.Text}' will be implemented here.", 
                "Configuration", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}

