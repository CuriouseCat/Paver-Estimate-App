using System.Text.Json;
using AllAroundEstimates.Models;

namespace AllAroundEstimates.Services;

public static class TimeCardStorage
{
    private const string FileName = "timecards.json";

    private static string FilePath =>
        Path.Combine(FileSystem.AppDataDirectory, FileName);

    public static List<TimeCard> LoadTimeCards()
    {
        if (!File.Exists(FilePath))
            return new List<TimeCard>();

        var json = File.ReadAllText(FilePath);
        if (string.IsNullOrWhiteSpace(json))
            return new List<TimeCard>();

        return JsonSerializer.Deserialize(json, TimeCardJsonContext.Default.ListTimeCard) ?? new List<TimeCard>();
    }

    public static void SaveTimeCards(List<TimeCard> timeCards)
    {
        var json = JsonSerializer.Serialize(timeCards, TimeCardJsonContext.Default.ListTimeCard);
        File.WriteAllText(FilePath, json);
    }

    public static DateTime GetWeekStart(DateTime date)
    {
        var daysSinceMonday = ((int)date.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
        return date.Date.AddDays(-daysSinceMonday);
    }

    /// <summary>
    /// The given employee's time card for the week containing `date`, or null if they haven't
    /// started one yet. Name matching is case-insensitive.
    /// </summary>
    public static TimeCard? FindTimeCard(string employeeName, DateTime date)
    {
        var weekStart = GetWeekStart(date);
        return LoadTimeCards().FirstOrDefault(t =>
            string.Equals(t.EmployeeName, employeeName, StringComparison.OrdinalIgnoreCase) &&
            t.WeekStartDate == weekStart);
    }

    public static void SaveTimeCard(TimeCard timeCard)
    {
        var timeCards = LoadTimeCards();
        var index = timeCards.FindIndex(t => t.Id == timeCard.Id);
        if (index >= 0)
        {
            timeCards[index] = timeCard;
        }
        else
        {
            timeCards.Add(timeCard);
        }

        SaveTimeCards(timeCards);
    }

    /// <summary>
    /// All of an employee's time cards, most recent week first, for the read-only history list.
    /// </summary>
    public static List<TimeCard> GetTimeCardsForEmployee(string employeeName)
    {
        return LoadTimeCards()
            .Where(t => string.Equals(t.EmployeeName, employeeName, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(t => t.WeekStartDate)
            .ToList();
    }
}
