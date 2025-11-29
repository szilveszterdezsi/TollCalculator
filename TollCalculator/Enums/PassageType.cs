using System.ComponentModel;

namespace TollCalculator.Enums
{
    public enum PassageType
    {
        [Description("Unknown")]
        Unknown,
        [Description("Standard (Per Fee Table)")]
        Standard,
        [Description("Exemption (No Fee Interval Applies)")]
        ExemptionNoFeeInterval,
        [Description("Exemption (Weekend)")]
        ExemptionWeekend,
        [Description("Exemption (Public Holiday)")]
        ExemptionPublicHoliday,
        [Description("Partial Exemption (Daily Maximum Reached)")]
        ExemptionPartialDailyMax,
        [Description("Full Exemption (Daily Maximum Reached)")]
        ExemptionFullDailyMax,
        [Description("Standard (Peak in Window)")]
        StandardWindowPeak,
        [Description("Exemption (Non-Peak in Window)")]
        ExemptionWindowNonPeak,
        [Description("Exemption for Exempt Vehicle Type")]
        ExemptionVehicleType
    }
}
