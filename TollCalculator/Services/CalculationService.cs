
using PublicHoliday;
using TollCalculator.Enums;
using TollCalculator.Models;

namespace TollCalculator.Services;

public class CalculationService
{
    private readonly SwedenPublicHoliday swedenPublicHoliday = new();
    private readonly RulesRepository? rulesRepository;

    private Rules defaultRules = new()
    {
        DailyMaxFee = 60,
        WindowDurationMinutes = 60,
        Fees =
            [
                new() { Amount = 8,
                    Intervals =
                    [
                        new() { StartTime = new(06, 00), EndTime = new(06, 30) },
                        new() { StartTime = new(08, 30), EndTime = new(15, 00) },
                        new() { StartTime = new(17, 00), EndTime = new(18, 00) }
                    ]
                },
                new()
                {
                    Amount = 13,
                    Intervals =
                    [
                        new() { StartTime = new(06, 30), EndTime = new(07, 00) },
                        new() { StartTime = new(08, 00), EndTime = new(08, 30) },
                        new() { StartTime = new(15, 00), EndTime = new(15, 30) }
                    ]
                },
                new()
                {
                    Amount = 18,
                    Intervals =
                    [
                        new() { StartTime = new(07, 00), EndTime = new(08, 00) },
                        new() { StartTime = new(15, 30), EndTime = new(17, 00) }
                    ]
                }
            ],
        ExemptDaysOfTheWeek = [DayOfWeek.Saturday, DayOfWeek.Sunday],
        ExemptVehicleTypes = [VehicleType.Emergency, VehicleType.Diplomat, VehicleType.Military],
        ValidUntil = new(2026, 12, 31)
    };

    public CalculationService() { }

    public CalculationService(RulesRepository rulesRepository) => this.rulesRepository = rulesRepository;

    public Rules Rules => defaultRules;

    public bool IsRulesRemoteSource { private set; get; } = false;

    public async Task<IEnumerable<DailyReport>> GetDailyReportsAsync(VehicleType vehicleType, IEnumerable<DateTime> dateTimes)
    {
        await CheckRulesValidityAsync();
        return dateTimes
            .GroupBy(date => DateOnly.FromDateTime(date.Date))
            .Select(dailyDateTimes =>
            {
                var Windows = GenerateWindows(dailyDateTimes);
                return new DailyReport
                {
                    Date = dailyDateTimes.Key,
                    Windows = EvaluateWindows(dailyDateTimes.Key, vehicleType, Windows)
                };
            })
            .OrderBy(report => report.Date);
    }

    private async Task CheckRulesValidityAsync()
    {
        if (rulesRepository != null && (DateTime.UtcNow > Rules.ValidUntil || !IsRulesRemoteSource))
        {
            defaultRules = await rulesRepository.GetRulesAsync();
            IsRulesRemoteSource = true;
        }
    }

    private IEnumerable<IEnumerable<Passage>> EvaluateWindows(DateOnly date, VehicleType vehicleType, IEnumerable<IEnumerable<Passage>> windows)
    {
        var dateTime = date.ToDateTime(TimeOnly.MinValue);
        return
            // if vehicle type is exempt or date is a holiday or weekend mark all passages to corresponding passage type
            // else fallback to evaluating all windows
            (IsPublicHoliday(dateTime), IsWeekend(date), IsExemptVehicle(vehicleType))
            switch
            {
                (_, _, true) => SetPassageTypeForAll(windows, PassageType.ExemptionVehicleType),
                (_, true, _) => SetPassageTypeForAll(windows, PassageType.ExemptionWeekend),
                (true, _, _) => SetPassageTypeForAll(windows, PassageType.ExemptionPublicHoliday),
                _ => EvaluateWindows(windows)
            };
    }

    private IEnumerable<IEnumerable<Passage>> EvaluateWindows(IEnumerable<IEnumerable<Passage>> windows)
    {
        double dailyTotal = 0.0;
        foreach (var window in windows)
        {
            // if window (for some reason) is empty, skip it
            if (!window.Any()) continue;

            // if window doesn't contain any potential fees, don't evalulate
            if (!window.Where(passage => passage.PotentialFee > 0).Any())
            {
                yield return window;
                continue;
            }

            // if daily max reached, mark all passages full exempt
            if (dailyTotal >= defaultRules.DailyMaxFee)
            {
                yield return SetPassageTypeForAll(window, PassageType.ExemptionFullDailyMax);
                continue;
            }

            // find peak fee and passage in window
            var peakFee = window.Max(p => p.PotentialFee);
            var peakPassage = window.First(p => p.PotentialFee == peakFee);

            // calculate remaining allowed incement for the day and charge to peak passage
            var allowedIncrement = Math.Min(peakFee, defaultRules.DailyMaxFee - dailyTotal);
            peakPassage.ChargedFee = allowedIncrement;

            // aggregate daily total
            dailyTotal += allowedIncrement;

            // if allowed increment is smaller than peak, mark passage partial excempt
            // else mark standard if alone in window
            // else standard (window peak)
            if (allowedIncrement < peakFee)
                peakPassage.Type = PassageType.ExemptionPartialDailyMax;
            else if (window.Count() == 1)
                peakPassage.Type = PassageType.Standard;
            else
                peakPassage.Type = PassageType.StandardWindowPeak;

            // mark any remaining passages with potential fees as exempt (window non-peak)
            foreach (var passage in window)
                if (!ReferenceEquals(passage, peakPassage) && passage.PotentialFee > 0)
                    passage.Type = PassageType.ExemptionWindowNonPeak;

            yield return window;
        }
    }

    private IEnumerable<IEnumerable<Passage>> GenerateWindows(IGrouping<DateOnly, DateTime> dailyDateTimes)
    {
        var passages = MapDateTimesToPassages(dailyDateTimes);
        if (!passages.Any()) yield break;

        var currentWindow = new List<Passage>();
        var windowDuration = TimeSpan.FromMinutes(defaultRules.WindowDurationMinutes);
        TimeOnly? currentWindowStartTime = null;

        foreach (var passage in passages)
        {
            currentWindowStartTime ??= passage.Time;
            // if window duration exceeded or passage has no potential fee close current window and open new
            var durationExcceded = (passage.Time - currentWindowStartTime) >= windowDuration;
            if (currentWindow.Count != 0 && (durationExcceded || passage.PotentialFee == 0))
            {
                yield return currentWindow;
                currentWindow = [];
                currentWindowStartTime = passage.Time;
            }
            currentWindow.Add(passage);
            // if passage has no potential fee close current window and open new
            // only track window duration when passage with fee is found
            if (passage.PotentialFee == 0)
            {
                yield return currentWindow;
                currentWindow = [];
                currentWindowStartTime = null;
            }
        }

        // return last window if not empty
        if (currentWindow.Count != 0)
            yield return currentWindow;
    }

    private IEnumerable<Passage> MapDateTimesToPassages(IGrouping<DateOnly, DateTime> dailyDateTimes)
        => [.. dailyDateTimes.OrderBy(dateTime => dateTime).Select(MapDateTimeToPassage)];

    private Passage MapDateTimeToPassage(DateTime dateTime)
    {
        var time = TimeOnly.FromDateTime(dateTime);
        var potentialFee = GetPotentialFee(time);
        var type = potentialFee == 0 ? PassageType.ExemptionNoFeeInterval : PassageType.Unknown;
        return new Passage { Type = type, Time = time, PotentialFee = potentialFee };
    }

    private double GetPotentialFee(TimeOnly time)
        => defaultRules.Fees.FirstOrDefault(fee => fee.Intervals.Any(interval => interval.Overlaps(time)))?.Amount ?? 0;

    private bool IsPublicHoliday(DateTime dateTime)
        => swedenPublicHoliday.IsPublicHoliday(dateTime);

    private bool IsWeekend(DateOnly date)
        => defaultRules.ExemptDaysOfTheWeek.Any(day => day.Equals(date.DayOfWeek));

    private bool IsExemptVehicle(VehicleType vehicleType)
        => defaultRules.ExemptVehicleTypes.Any(vehicle => vehicle.Equals(vehicleType));

    private static IEnumerable<IEnumerable<Passage>> SetPassageTypeForAll(IEnumerable<IEnumerable<Passage>> windows, PassageType type)
        => windows.Select(window => SetPassageTypeForAll(window, type));

    private static IEnumerable<Passage> SetPassageTypeForAll(IEnumerable<Passage> window, PassageType type)
        => window.Select(passage => { passage.Type = type; return passage; });
}
