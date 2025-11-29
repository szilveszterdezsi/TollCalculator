
namespace TollCalculator.Models;

public class Fee
{
    public double Amount { get; set; }
    public IEnumerable<Interval> Intervals { get; set; } = [];
}
