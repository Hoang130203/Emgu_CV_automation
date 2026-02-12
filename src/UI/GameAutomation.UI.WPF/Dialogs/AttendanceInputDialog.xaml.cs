using GameAutomation.Core.Workflows.Examples;
using System.Windows;

namespace GameAutomation.UI.WPF.Dialogs;

public partial class AttendanceInputDialog : Window
{
    public string? SheetName { get; private set; }
    public int StartRow { get; private set; } = 2;

    public AttendanceInputDialog()
    {
        InitializeComponent();

        // Display effective date with 5 AM rule
        var effectiveDate = NthAttendanceWorkflow.GetEffectiveDate();
        EffectiveDateLabel.Text = effectiveDate.ToString("dd/MM/yyyy (dddd)");

        StartRowTextBox.Focus();
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        if (!int.TryParse(StartRowTextBox.Text, out int startRow) || startRow < 1)
        {
            MessageBox.Show("Please enter a valid start row (>= 1)", "Invalid Input",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            StartRowTextBox.Focus();
            return;
        }

        if (string.IsNullOrWhiteSpace(SheetNameTextBox.Text))
        {
            MessageBox.Show("Please enter a sheet name", "Invalid Input",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            SheetNameTextBox.Focus();
            return;
        }

        SheetName = SheetNameTextBox.Text.Trim();
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
