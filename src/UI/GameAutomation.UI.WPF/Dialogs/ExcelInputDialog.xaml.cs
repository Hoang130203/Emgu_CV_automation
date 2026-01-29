using System.Windows;

namespace GameAutomation.UI.WPF.Dialogs;

/// <summary>
/// Dialog for inputting Excel sheet name and start row
/// </summary>
public partial class ExcelInputDialog : Window
{
    public string? SheetName { get; private set; }
    public int StartRow { get; private set; } = 1;

    public ExcelInputDialog()
    {
        InitializeComponent();
        StartRowTextBox.Focus();
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        // Validate start row
        if (!int.TryParse(StartRowTextBox.Text, out int startRow) || startRow < 1)
        {
            MessageBox.Show("Please enter a valid start row (>= 1)", "Invalid Input",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            StartRowTextBox.Focus();
            return;
        }

        SheetName = string.IsNullOrWhiteSpace(SheetNameTextBox.Text) ? null : SheetNameTextBox.Text.Trim();
        StartRow = startRow;

        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
