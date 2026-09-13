using AllAroundEstimates.Models;
using AllAroundEstimates.Services;

namespace AllAroundEstimates.Views;

public partial class TimeCardPage : ContentPage
{
    private static readonly DayOfWeek[] WeekDayOrder =
    {
        DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday,
        DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday
    };

    private TimeCard? _currentTimeCard;

    public TimeCardPage()
    {
        InitializeComponent();
    }

    private async void OnFindClicked(object sender, EventArgs e)
    {
        var name = EmployeeNameEntry.Text?.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            await DisplayAlertAsync("Name Required", "Please enter your name first.", "OK");
            return;
        }

        try
        {
            var existing = TimeCardStorage.FindTimeCard(name, DateTime.Now);
            _currentTimeCard = existing ?? new TimeCard
            {
                EmployeeName = name,
                WeekStartDate = TimeCardStorage.GetWeekStart(DateTime.Now)
            };

            BuildDayRows(_currentTimeCard);
            RefreshHistory(name);
            TimeCardForm.IsVisible = true;
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", ex.Message, "OK");
        }
    }

    private void BuildDayRows(TimeCard timeCard)
    {
        WeekRangeLabel.Text = timeCard.WeekRangeText;
        LockedNoticeLabel.IsVisible = !timeCard.IsEditable;
        SaveButton.IsVisible = timeCard.IsEditable;

        DaysContainer.Children.Clear();

        var today = DateTime.Now.Date;

        for (var i = 0; i < WeekDayOrder.Length; i++)
        {
            var dayOfWeek = WeekDayOrder[i];
            var dayAttendance = timeCard.GetDay(dayOfWeek);
            var dayDate = timeCard.WeekStartDate.AddDays(i);
            var isToday = dayDate == today;

            // Highlight today with a colored outline rather than a filled background, so there's
            // no risk of theme-dependent text-color contrast issues (a filled light background
            // previously made text invisible in dark mode).
            var dayCard = new Border
            {
                Stroke = isToday ? Colors.MediumPurple : Colors.LightGray,
                StrokeThickness = isToday ? 2 : 1,
                Padding = new Thickness(12, 10)
            };

            var row = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition(GridLength.Star),
                    new ColumnDefinition(GridLength.Auto)
                }
            };

            var dayLabel = new Label
            {
                Text = $"{dayOfWeek} ({dayDate:MMM d}){(isToday ? "  •  Today" : string.Empty)}",
                VerticalOptions = LayoutOptions.Center,
                FontAttributes = FontAttributes.Bold
            };
            Grid.SetColumn(dayLabel, 0);

            var statusButton = new Button
            {
                WidthRequest = 110,
                FontSize = 13,
                IsEnabled = timeCard.IsEditable
            };
            ApplyStatusStyle(statusButton, dayAttendance.Status);

            statusButton.Clicked += (_, _) =>
            {
                dayAttendance.Status = NextStatus(dayAttendance.Status);
                ApplyStatusStyle(statusButton, dayAttendance.Status);
                UpdateSummary(timeCard);
            };

            Grid.SetColumn(statusButton, 1);

            row.Add(dayLabel);
            row.Add(statusButton);

            dayCard.Content = row;
            DaysContainer.Children.Add(dayCard);
        }

        UpdateSummary(timeCard);
    }

    private static AttendanceStatus NextStatus(AttendanceStatus current) => current switch
    {
        AttendanceStatus.NotMarked => AttendanceStatus.Present,
        AttendanceStatus.Present => AttendanceStatus.Absent,
        AttendanceStatus.Absent => AttendanceStatus.NotMarked,
        _ => AttendanceStatus.NotMarked
    };

    private static void ApplyStatusStyle(Button button, AttendanceStatus status)
    {
        (button.Text, button.BackgroundColor, button.TextColor) = status switch
        {
            AttendanceStatus.Present => ("Present", Colors.Green, Colors.White),
            AttendanceStatus.Absent => ("Absent", Colors.Red, Colors.White),
            _ => ("Not Marked", Colors.LightGray, Colors.Black)
        };
    }

    private void UpdateSummary(TimeCard timeCard)
    {
        AttendanceSummaryLabel.Text = timeCard.AttendanceSummaryText;
    }

    private void RefreshHistory(string employeeName)
    {
        try
        {
            HistoryList.ItemsSource = TimeCardStorage.GetTimeCardsForEmployee(employeeName);
        }
        catch
        {
            HistoryList.ItemsSource = new List<TimeCard>();
        }
    }

    private async void OnSaveClicked(object sender, EventArgs e)
    {
        if (_currentTimeCard is null)
            return;

        try
        {
            TimeCardStorage.SaveTimeCard(_currentTimeCard);
            UpdateSummary(_currentTimeCard);
            RefreshHistory(_currentTimeCard.EmployeeName);
            await DisplayAlertAsync("Saved", "Time card saved.", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", ex.Message, "OK");
        }
    }

    private async void OnExportClicked(object sender, EventArgs e)
    {
        if (_currentTimeCard is null)
        {
            await DisplayAlertAsync("No Time Card", "Find or start a time card first.", "OK");
            return;
        }

        try
        {
            using var stream = new MemoryStream();
            PdfGenerator.GenerateTimeCardPdf(stream, _currentTimeCard);

            var safeName = string.Join("_", _currentTimeCard.EmployeeName.Split(Path.GetInvalidFileNameChars()));
            var fileName = $"TimeCard_{safeName}_{_currentTimeCard.WeekStartDate:yyyyMMdd}.pdf";
            await PdfExportService.ExportPdfAsync(fileName, stream);
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Export Failed", ex.Message, "OK");
        }
    }
}
