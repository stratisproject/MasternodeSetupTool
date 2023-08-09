using System.Windows;

namespace MasternodeSetupTool
{
    /// <summary>
    /// Interaction logic for ConfirmationDialog.xaml
    /// </summary>
    public partial class ConfirmationDialog : Window
    {
        public ConfirmationDialog(string labelText, string firstTextContent, bool firstTextEditable)
        {
            InitializeComponent();

            this.Label1.Content = labelText;
            this.Text1.Text = firstTextContent;
            this.Text1.IsReadOnly = !firstTextEditable;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(this.Text1.Text) && !string.IsNullOrWhiteSpace(this.Text2.Text) && this.Text1.Text == this.Text2.Text)
            {
                this.Close();
            }

            MessageBox.Show("Please ensure the two text boxes match!", "Error");
        }
    }
}
