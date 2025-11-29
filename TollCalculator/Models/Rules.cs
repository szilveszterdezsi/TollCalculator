
using TollCalculator.Enums;

namespace TollCalculator.Models;

public class Rules
{
    public double DailyMaxFee { get; set; }
    public double WindowDurationMinutes { get; set; }
    public IEnumerable<Fee> Fees { get; set; } = [];
    public IEnumerable<DayOfWeek> ExemptDaysOfTheWeek { get; set; } = [];
    public IEnumerable<VehicleType> ExemptVehicleTypes { get; set; } = [];
    public DateTime ValidUntil { get; set; }
}
