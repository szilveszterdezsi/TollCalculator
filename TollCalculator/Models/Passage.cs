
using TollCalculator.Enums;

namespace TollCalculator.Models;

public class Passage
{
    public PassageType Type { get; set; }
    public TimeOnly Time { get; set;}
    public double PotentialFee { get; set; }
    public double ChargedFee { get; set; }
}