using System.Windows;
using System.Windows.Controls;

namespace MasternodeSetupTool
{
    public class InputBox
    {
        private readonly Window box;
        private readonly TextBox input;

        private bool clicked;

        public InputBox(string content, string inputBoxTitle = "", string initialText = "")
        {
            var sp1 = new StackPanel();

            sp1.Children.Add(new TextBlock
            {
                TextWrapping = TextWrapping.Wrap,
                HorizontalAlignment = HorizontalAlignment.Center,
                Text = content
            });

            this.input = new TextBox()
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                Text = initialText,
                MinWidth = 280,
            };
            
            sp1.Children.Add(this.input);

            var ok = new Button
            {
                Width = 70,
                Height = 30,
                Content = "Ok",
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(2)
            };

            ok.Click += Ok_Click;
            sp1.Children.Add(ok);

            this.box = new Window
            {
                Height = 300,
                Width = 300,
                Title = inputBoxTitle,
                Content = sp1
            };

            this.box.Closing += Box_Closing;
        }

        private void Box_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            this.box.DialogResult = this.clicked;
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            this.clicked = true;
            this.box.Close();
            this.clicked = false;
        }

        public string? ShowDialog()
        {
            this.box.ShowDialog();

            if (this.box.DialogResult != true) 
                return null;

            return this.input.Text;
        }
    }
}
