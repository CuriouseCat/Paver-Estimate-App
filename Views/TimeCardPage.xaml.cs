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

    private readonly Dictionary<DayOfWeek, Entry> _hoursEntries = new();
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
        _hoursEntries.Clear();

        var today = DateTime.Now.Date;

        for (var i = 0; i < WeekDayOrder.Length; i++)
        {
            var dayOfWeek = WeekDayOrder[i];
            var dayPunch = timeCard.GetDay(dayOfWeek);
            var dayDate = timeCard.WeekStartDate.AddDays(i);
            var isToday = dayDate == today;

            var dayCard = new Border
            {
                Stroke = Colors.LightGray,
                StrokeThickness = 1,
                Padding = new Thickness(10, 8),
                BackgroundColor = isToday ? Color.FromArgb("#F0F4FF") : Colors.Transparent
            };

            var stack = new VerticalStackLayout { Spacing = 4 };

            stack.Add(new Label
            {
                Text = $"{dayOfWeek} ({dayDate:MMM d}){(isToday ? "  •  Today" : string.Empty)}",
                FontAttributes = FontAttributes.Bold
            });

            var statusLabel = new Label
            {
                Text = FormatPunchStatus(dayPunch),
                FontSize = 12,
                TextColor = Colors.Gray
            };
            stack.Add(statusLabel);

            if (isToday && timeCard.IsEditable)
            {
                var buttonRow = new HorizontalStackLayout { Spacing = 10 };

                var clockInButton = new Button
                {
                    Text = "Clock In",
                    FontSize = 12,
                    Padding = new Thickness(12, 6),
                    IsEnabled = dayPunch.ClockInTime is null
                };

                var clockOutButton = new Button
                {
                    Text = "Clock Out",
                    FontSize = 12,
                    Padding = new Thickness(12, 6),
                    IsEnabled = dayPunch.ClockInTime is not null && dayPunch.ClockOutTime is null
                };

                clockInButton.Clicked += (_, _) =>
                {
                    dayPunch.ClockInTime = DateTime.Now;
                    dayPunch.ClockOutTime = null;
                    statusLabel.Text = FormatPunchStatus(dayPunch);
                    clockInButton.IsEnabled = false;
                    clockOutButton.IsEnabled = true;
                };

                clockOutButton.Clicked += (_, _) =>
                {
                    if (dayPunch.ClockInTime is null)
                        return;

                    dayPunch.ClockOutTime = DateTime.Now;
                    dayPunch.Hours = (decimal)(dayPunch.ClockOutTime.Value - dayPunch.ClockInTime.Value).TotalHours;

                    statusLabel.Text = FormatPunchStatus(dayPunch);
                    clockOutButton.IsEnabled = false;

                    if (_hoursEntries.TryGetValue(dayOfWeek, out var hoursEntry))
                    {
                        hoursEntry.Text = dayPunch.Hours.ToString("0.##");
                    }

                    UpdateTotal(_currentTimeCard!);
                };

                buttonRow.Add(clockInButton);
                buttonRow.Add(clockOutButton);
                stack.Add(buttonRow);
            }

            var hoursRow = new HorizontalStackLayout { Spacing = 8 };
            hoursRow.Add(new Label { Text = "Hours:", VerticalOptions = LayoutOptions.Center });
            var hoursEntry = new Entry
            {
                Text = dayPunch.Hours.ToString("0.##"),
                Keyboard = Keyboard.Numeric,
                WidthRequest = 80,
                IsReadOnly = !timeCard.IsEditable
            };
            _hoursEntries[dayOfWeek] = hoursEntry;
            hoursRow.Add(hoursEntry);
            stack.Add(hoursRow);

            dayCard.Content = stack;
            DaysContainer.Children.Add(dayCard);
        }

        UpdateTotal(timeCard);
    }

    private static string FormatPunchStatus(DayPunch punch)
    {
        if (punch.ClockInTime is null)
            return "Not clocked in";

        var inText = punch.ClockInTime.Value.ToString("h:mm tt");
        var outText = punch.ClockOutTime?.ToString("h:mm tt") ?? "still clocked in";
        return $"In: {inText}   Out: {outText}";
    }

    private void UpdateTotal(TimeCard timeCard)
    {
        TotalHoursLabel.Text = $"Total Hours: {timeCard.TotalHours:N1}";
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

        foreach (var dayOfWeek in WeekDayOrder)
        {
            if (!_hoursEntries.TryGetValue(dayOfWeek, out var entry))
                continue;

            if (!decimal.TryParse(entry.Text, out var hours))
            {
                await DisplayAlertAsync("Invalid Input", $"Please enter a valid number of hours for {dayOfWeek}.", "OK");
                return;
            }

            _currentTimeCard.GetDay(dayOfWeek).Hours = hours;
        }

        try
        {
            TimeCardStorage.SaveTimeCard(_currentTimeCard);
            UpdateTotal(_currentTimeCard);
            RefreshHistory(_currentTimeCard.EmployeeName);
            await DisplayAlertAsync("Saved", "Time card saved.", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", ex.Message, "OK");
        }
    }
}
