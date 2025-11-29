using TollCalculator.Enums;
using TollCalculator.Models;
using TollCalculator.Services;

namespace TollCalculator.Tests
{
    public class CalculationServiceTests
    {
        private static readonly Rules rules = new CalculationService().Rules;
        private CalculationService calculatonService;

        [SetUp]
        public void Setup()
        {
            calculatonService = new();
        }

        private static IEnumerable<TestCaseData> SinglePassageTypeEvaluationTestCases() =>
        [
            new TestCaseData(VehicleType.Car, new DateTime[] { new (2025, 12, 19, 08, 0, 0) }, PassageType.Standard),
            new TestCaseData(VehicleType.Car, new DateTime[] { new (2025, 12, 19, 18, 0, 0) }, PassageType.ExemptionNoFeeInterval),
            new TestCaseData(VehicleType.Car, new DateTime[] { new (2025, 12, 20, 08, 0, 0) }, PassageType.ExemptionWeekend),
            new TestCaseData(VehicleType.Car, new DateTime[] { new (2025, 12, 24, 08, 0, 0) }, PassageType.ExemptionPublicHoliday),
            new TestCaseData(VehicleType.Emergency, new DateTime[] { new (2025, 12, 19, 08, 0, 0) }, PassageType.ExemptionVehicleType),
            new TestCaseData(VehicleType.Emergency, new DateTime[] { new (2025, 12, 19, 18, 0, 0) }, PassageType.ExemptionVehicleType),
            new TestCaseData(VehicleType.Emergency, new DateTime[] { new (2025, 12, 20, 08, 0, 0) }, PassageType.ExemptionVehicleType),
            new TestCaseData(VehicleType.Emergency, new DateTime[] { new (2025, 12, 24, 08, 0, 0) }, PassageType.ExemptionVehicleType),
            .. DailyRangeSinglePassageTestCases(VehicleType.Emergency, new (2025, 12, 19, 08, 0, 0), 60, PassageType.ExemptionVehicleType)
        ];

        private static IEnumerable<DateTime> DailyRangeDateTimes(DateTime dateTime, int intervalMinutes)
            => Enumerable.Range(0, 24 * 60 / intervalMinutes).Select(i => dateTime.Date.AddMinutes(i * intervalMinutes));

        private static IEnumerable<TestCaseData> DailyRangeSinglePassageTestCases(VehicleType vehicleType, DateTime dateTime, int intervalMinutes, PassageType expected)
            => DailyRangeDateTimes(dateTime, intervalMinutes).Select(datetime => new TestCaseData(vehicleType, new DateTime[] { datetime }, expected));

        [Test, TestCaseSource(nameof(SinglePassageTypeEvaluationTestCases))]
        public async Task TestSinglePassageTypeEvaluation(VehicleType vehicleType, IEnumerable<DateTime> dateTimes, PassageType expected)
        {
            var result = await calculatonService.GetDailyReportsAsync(vehicleType, dateTimes);
            var passage = result.First().Windows.First().First();
            PrettyPrintDailyReports(result);
            Assert.That(passage.Type, Is.EqualTo(expected));
        }

        private static IEnumerable<TestCaseData> MultiPassageWindowGenerationTestCases() =>
        [
            new TestCaseData(VehicleType.Car, DailyRangeDateTimes(new(2025, 12, 19), 10), 84),
            new TestCaseData(VehicleType.Car, DailyRangeDateTimes(new(2025, 12, 19), 15), 60),
            new TestCaseData(VehicleType.Car, DailyRangeDateTimes(new(2025, 12, 19), 30), 36),
            new TestCaseData(VehicleType.Car, DailyRangeDateTimes(new(2025, 12, 19), 60), 24),
            new TestCaseData(VehicleType.Car, DailyRangeDateTimes(new(2025, 12, 19), 120), 12),
            new TestCaseData(VehicleType.Car, DailyRangeDateTimes(new(2025, 12, 19), 240), 6)
        ];

        [Test, TestCaseSource(nameof(MultiPassageWindowGenerationTestCases))]
        public async Task TestMultiPassageWindowGeneration(VehicleType vehicleType, IEnumerable<DateTime> dateTimes, int expectedWindowCount)
        {
            var result = await calculatonService.GetDailyReportsAsync(vehicleType, dateTimes);
            var windows = result.First().Windows;
            PrettyPrintDailyReports(result);
            Assert.That(windows.Count(), Is.EqualTo(expectedWindowCount));
        }

        private static IEnumerable<(DateTime, double)> DateTimesAndFeesFromFeeRules(DateTime dateTime)
            => rules.Fees.SelectMany(fee => fee.Intervals.Select(interval => (dateTime.Date + interval.StartTime.ToTimeSpan(), fee.Amount)));

        private static IEnumerable<TestCaseData> GenerateSinglePassageExpectedFeeTestCases(VehicleType vehicleType, DateTime dateTime, double? expectedFee = null)
            => DateTimesAndFeesFromFeeRules(dateTime).Select(kvp => new TestCaseData(vehicleType, new DateTime[] { kvp.Item1 }, expectedFee ?? kvp.Item2));

        private static IEnumerable<TestCaseData> SinglePassageExpectedFeeTestCases() =>
        [
            .. GenerateSinglePassageExpectedFeeTestCases(VehicleType.Car, new (2025, 12, 19)),
            .. GenerateSinglePassageExpectedFeeTestCases(VehicleType.Emergency, new (2025, 12, 19), 0)
        ];

        [Test, TestCaseSource(nameof(SinglePassageExpectedFeeTestCases))]
        public async Task TestSinglePassageExpectedFee(VehicleType vehicleType, IEnumerable<DateTime> dateTimes, double expectedFee)
        {
            var result = await calculatonService.GetDailyReportsAsync(vehicleType, dateTimes);
            var passage = result.First().Windows.First().First();
            PrettyPrintDailyReports(result);
            Assert.That(passage.ChargedFee, Is.EqualTo(expectedFee));
        }

        private static IEnumerable<DateTime> PeakChargeInWindowVariedFeeDateTimes() =>
        [
            new (2025, 12, 19, 06, 29, 0),
            new (2025, 12, 19, 06, 31, 0),
            new (2025, 12, 19, 07, 15, 0)
        ];

        private static IEnumerable<DateTime> PeakChargeInWindowSameFeeDateTimes() =>
        [
            new (2025, 12, 19, 06, 15, 0),
            new (2025, 12, 19, 06, 16, 0),
            new (2025, 12, 19, 06, 17, 0),
        ];

        private static IEnumerable<TestCaseData> PeakChargeInWindowFeeEvaluationTestCases() =>
        [
            new TestCaseData(VehicleType.Car, PeakChargeInWindowVariedFeeDateTimes(), 18),
            new TestCaseData(VehicleType.Car, PeakChargeInWindowSameFeeDateTimes(), 8)
        ];

        [Test, TestCaseSource(nameof(PeakChargeInWindowFeeEvaluationTestCases))]
        public async Task TestPeakChargeInWindowFeeEvaluation(VehicleType vehicleType, IEnumerable<DateTime> dateTimes, double expectedDailyTotal)
        {
            var result = await calculatonService.GetDailyReportsAsync(vehicleType, dateTimes);
            var dailyReport = result.First();
            PrettyPrintDailyReports(result);
            Assert.That(dailyReport.TotalFee, Is.EqualTo(expectedDailyTotal));
        }

        private static IEnumerable<TestCaseData> PeakChargeInWindowPassageTypeEvaluationTestCases() =>
        [
            new TestCaseData(VehicleType.Car, PeakChargeInWindowVariedFeeDateTimes(), 18, PassageType.StandardWindowPeak, PassageType.ExemptionWindowNonPeak),
            new TestCaseData(VehicleType.Car, PeakChargeInWindowSameFeeDateTimes(), 8, PassageType.StandardWindowPeak, PassageType.ExemptionWindowNonPeak)
        ];

        [Test, TestCaseSource(nameof(PeakChargeInWindowPassageTypeEvaluationTestCases))]
        public async Task TestPeakChargeInWindowPassageTypeEvaluation(
            VehicleType vehicleType, IEnumerable<DateTime> dateTimes, double chargedFee, PassageType peakPassageType, PassageType remaining)
        {
            var result = await calculatonService.GetDailyReportsAsync(vehicleType, dateTimes);
            var passages = result.First().Windows.First();
            PrettyPrintDailyReports(result);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(passages.Any(p => p.Type.Equals(peakPassageType) && p.ChargedFee.Equals(chargedFee)), Is.True);
                Assert.That(passages.Where(p => !p.Type.Equals(peakPassageType)).All(p => p.Type.Equals(remaining)), Is.True);
            }
        }

        private static IEnumerable<TestCaseData> DailyMaxTotalCapTestCases() =>
        [
            new TestCaseData(VehicleType.Car, DailyRangeDateTimes(new(2025, 12, 19), 60), rules.DailyMaxFee),
            new TestCaseData(VehicleType.Car, DailyRangeDateTimes(new(2025, 12, 24), 60), rules.DailyMaxFee)
        ];

        [Test, TestCaseSource(nameof(DailyMaxTotalCapTestCases))]
        public async Task TestDailyMaxTotalCap(VehicleType vehicleType, IEnumerable<DateTime> dateTimes, double dailyMaxTotal)
        {
            var result = await calculatonService.GetDailyReportsAsync(vehicleType, dateTimes);
            var dailyReport = result.First();
            PrettyPrintDailyReports(result);
            Assert.That(dailyReport.TotalFee, Is.AtMost(dailyMaxTotal));
        }

        private static void PrettyPrintDailyReports(IEnumerable<DailyReport> dailyReports)
        {
            foreach (var dailyReport in dailyReports)
            {
                Console.WriteLine($"┌─ Daily Report ({dailyReport.Date:yyyy-MM-dd})");
                foreach (var window in dailyReport.Windows)
                {
                    foreach (var (pass, i) in window.Select((item, i) => (item, i)))
                    {
                        var branch = window.Count() == 1 ? " ─" : i == 0 ? "┌─" : i == window.Count() - 1 ? "└─" : "│ ";
                        Console.WriteLine($"│{branch} {pass.Time,-5} | Potential {pass.PotentialFee,3} | Charged {pass.ChargedFee,3} | {pass.Type,-10}");
                    }
                }
                Console.WriteLine($"└─ Total Fee: {dailyReport.TotalFee} SEK");
            }
        }

        private static IEnumerable<TestCaseData> SingleCarPassageTestCases()
        {
            // arrange
            yield return new TestCaseData(VehicleType.Car, new DateTime[] { new(2025, 12, 19, 08, 0, 0) }, 13);
        }

        [Test, TestCaseSource(nameof(SingleCarPassageTestCases))] 
        public async Task TestSingleCarPassage(VehicleType vehicleType, IEnumerable<DateTime> dateTimes, double expected)
        {
            // act
            var result = await calculatonService.GetDailyReportsAsync(vehicleType, dateTimes);

            PrettyPrintDailyReports(result); // optional pretty print for checking result

            // assert
            var passage = result.First().Windows.First().First();
            Assert.That(passage.ChargedFee, Is.EqualTo(expected)); // expected fee for this passage
        }
    }
}
