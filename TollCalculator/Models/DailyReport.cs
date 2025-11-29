
namespace TollCalculator.Models;

public class DailyReport
{
    public DateOnly Date { get; set; }
    public IEnumerable<IEnumerable<Passage>> Windows { get; set; } = [];
    public double TotalFee => Windows
        .SelectMany(window => window)
        .Sum(window => window.ChargedFee);
}
    
