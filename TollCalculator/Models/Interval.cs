
namespace TollCalculator.Models;

public class Interval
{
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public bool Overlaps(TimeOnly t) => t >= StartTime && t < EndTime;
}
