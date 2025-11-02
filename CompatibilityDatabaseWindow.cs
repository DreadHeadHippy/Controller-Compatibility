using System.Windows;

namespace ControllerCompatibility
{
    public partial class CompatibilityDatabaseWindow : Window
    {
        public CompatibilityDatabaseWindow()
        {
            InitializeComponent();
            CloseButton.Click += (s, e) => Close();
        }
    }
}