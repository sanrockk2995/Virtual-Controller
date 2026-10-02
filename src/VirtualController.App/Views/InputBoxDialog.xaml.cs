using System.Windows;

namespace VirtualController.App.Views
{
    public partial class InputBoxDialog : Window
    {
        public string ResponseText { get; private set; } = "";

        public InputBoxDialog(string prompt, string title, string defaultText = "")
        {
            InitializeComponent();
            Title = title;
            TxtPrompt.Text = prompt;
            TxtInput.Text = defaultText;
            TxtInput.SelectAll();
            TxtInput.Focus();
        }

        public static string? Show(Window owner, string prompt, string title, string defaultText = "")
        {
            var dlg = new InputBoxDialog(prompt, title, defaultText)
            {
                Owner = owner
            };
            if (dlg.ShowDialog() == true)
            {
                return dlg.ResponseText;
            }
            return null;
        }

        private void BtnOK_Click(object sender, RoutedEventArgs e)
        {
            ResponseText = TxtInput.Text.Trim();
            DialogResult = true;
            Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
