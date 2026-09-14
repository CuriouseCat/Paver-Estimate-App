namespace AllAroundEstimates.Views;

public partial class SquareFootageCalculatorPage : ContentPage
{
    private readonly List<(Entry Length, Entry Width)> _sections = new();
    private readonly Action<decimal> _onUseTotal;

    public SquareFootageCalculatorPage(Action<decimal> onUseTotal)
    {
        InitializeComponent();
        _onUseTotal = onUseTotal;
        AddSectionRow();
    }

    private void OnAddSectionClicked(object sender, EventArgs e) => AddSectionRow();

    private void AddSectionRow()
    {
        var lengthEntry = new Entry { Placeholder = "Length (ft)", Keyboard = Keyboard.Numeric, WidthRequest = 90 };
        var widthEntry = new Entry { Placeholder = "Width (ft)", Keyboard = Keyboard.Numeric, WidthRequest = 90 };
        var lineTotalLabel = new Label { Text = "= 0 sq ft", VerticalOptions = LayoutOptions.Center, WidthRequest = 85 };

        var removeButton = new Button
        {
            Text = "✕",
            FontSize = 12,
            Padding = new Thickness(8, 4),
            BackgroundColor = Colors.Transparent,
            TextColor = Colors.Red
        };

        void Recalc()
        {
            var length = decimal.TryParse(lengthEntry.Text, out var l) ? l : 0;
            var width = decimal.TryParse(widthEntry.Text, out var w) ? w : 0;
            lineTotalLabel.Text = $"= {length * width:N0} sq ft";
            RecalculateGrandTotal();
        }

        lengthEntry.TextChanged += (_, _) => Recalc();
        widthEntry.TextChanged += (_, _) => Recalc();

        var row = new HorizontalStackLayout { Spacing = 8 };
        row.Add(lengthEntry);
        row.Add(new Label { Text = "x", VerticalOptions = LayoutOptions.Center });
        row.Add(widthEntry);
        row.Add(lineTotalLabel);
        row.Add(removeButton);

        removeButton.Clicked += (_, _) =>
        {
            SectionsContainer.Children.Remove(row);
            _sections.RemoveAll(s => s.Length == lengthEntry);
            RecalculateGrandTotal();
        };

        SectionsContainer.Children.Add(row);
        _sections.Add((lengthEntry, widthEntry));
    }

    private void RecalculateGrandTotal()
    {
        TotalLabel.Text = ComputeTotal().ToString("N0");
    }

    private decimal ComputeTotal()
    {
        decimal total = 0;
        foreach (var (lengthEntry, widthEntry) in _sections)
        {
            var length = decimal.TryParse(lengthEntry.Text, out var l) ? l : 0;
            var width = decimal.TryParse(widthEntry.Text, out var w) ? w : 0;
            total += length * width;
        }
        return total;
    }

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        await Navigation.PopModalAsync();
    }

    private async void OnUseTotalClicked(object sender, EventArgs e)
    {
        var total = ComputeTotal();
        await Navigation.PopModalAsync();
        _onUseTotal(total);
    }
}
