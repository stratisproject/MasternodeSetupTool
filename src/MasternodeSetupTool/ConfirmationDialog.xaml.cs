using System.Windows;

namespace MasternodeSetupTool
{
    /// <summary>
    /// Interaction logic for ConfirmationDialog.xaml
    /// </summary>
    public partial class ConfirmationDialog : Window
    {
        private bool AllowEmpty;

        public ConfirmationDialog(string titleText, string labelText, string firstTextContent, bool firstTextEditable, bool allowEmpty = false)
        {
            InitializeComponent();

            this.Title = titleText;
            this.Label1.Content = labelText;
            this.Text1.Text = firstTextContent;
            this.Text1.IsReadOnly = !firstTextEditable;
            this.AllowEmpty = allowEmpty;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (string.Equals(this.Text1.Text, this.Text2.Text))
            {
                if (!this.AllowEmpty && (string.IsNullOrWhiteSpace(this.Text1.Text) || string.IsNullOrWhiteSpace(this.Text2.Text)))
                {
                    MessageBox.Show("Please ensure the fields are not empty!", "Error");
                    return;
                }
                this.Close();
            } else
            {
                MessageBox.Show("Please ensure the two text boxes match!", "Error");
            }
        }
    }
}
