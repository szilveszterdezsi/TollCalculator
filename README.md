# Toll Calculator 2.0.0-RC1
A web tool to calculate toll fees for one or more days, accounting for vehicle type, time intervals and exemptions.

## Development Environment
* **Microsoft Visual Studio 2026** IDE
* **C# 14** (with .NET 10 SDK & Runtime)
* **Blazor WebAssembly Standalone App** project template
* **NUnit** unit testing framework

## Calculation Rules
* The maximum daily total fee is capped at `60 SEK` (partial fees may apply)
* For multiple fees within the same `60-minute window`, only the peak fee applies
* Exempt vehicle types: `Emergency`, `Diplomat` and `Military`
* Exempt days of the week (weekends): `Saturday` and `Sunday`
* All public holidays are also exempt

## Project Structure
```text
TollCalculator
├─ TollCalculator/
│  ├─ Enums/
│  │  ├─ PassageType.cs            <- passage classification enum
│  │  └─ VehicleType.cs            <- vehicle classification enum
│  ├─ Extensions/
│  │  └─ EnumExtensions.cs         <- used to get description for enum type
│  ├─ Layout/
│  │  └─ MainLayout.razor
│  ├─ Models/
│  │  ├─ DailyReport.cs            <- all toll passages for a day
│  │  ├─ Fee.cs                    <- all intervals when amount applies
│  │  ├─ Interval.cs               <- interval when a fee applies
│  │  ├─ Passage.cs                <- time of passage and fee info
│  │  └─ Rules.cs                  <- all rules for calculation logic
│  ├─ Pages/
│  │  ├─ Calculator.razor          <- start page (SPA)
│  │  └─ NotFound.razor            <- 404 error page
│  ├─ Properties/
│  │  └─ launchSettings.json
│  ├─ Services/
│  │  ├─ CalculationService.cs     <- calculation logic
│  │  └─ RulesRepository.cs        <- fetches rules
│  ├─ wwwroot/
│  │  ├─ css
│  │  │  └─ app.css
│  │  ├─ data
│  │  │  └─ rules.json             <- calculation rules
│  │  ├─ icon-102.png
│  │  └─ index.html
│  ├─ _Imports.razor
│  ├─ App.razor
│  ├─ Program.cs
│  └─ TollFeeCalculator.csproj
├─ TollCalculator.Tests/
│  ├─ CalculationServiceTests.cs   <- calculation tests
│  └─ TollCalculator.Tests.csproj
└─ TollFeeCalculator.slnx
```

## Usage

1. Clone this repository
1. Build the project in **Visual Studio 2026** or using the **.NET 10 SDK**.
1. Run the `TollCalculator` project and open localhost url in your browser
1. Use the web interface to enter vehicle type and passage dates and times to calculate toll fees

### Optional: Adding a Test

You can write a new NUnit test in `CalculationServiceTests` for the `GetDailyReportsAsync`-method in `CalculationService` to verify calculations. For example:

```cs
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

    PrettyPrintDailyReports(result); // optional pretty print for human checking result

    // assert
    var passage = result.First().Windows.First().First();
    Assert.That(passage.ChargedFee, Is.EqualTo(expected)); // expected fee for this passage
}
```

## Dependencies (NuGet Packages)
* **[Microsoft.AspNetCore.Components.WebAssembly 10.0.0](https://www.nuget.org/packages/Microsoft.AspNetCore.Components.WebAssembly)** – Blazor WebAssembly runtime
* **[Microsoft.AspNetCore.Components.WebAssembly.DevServer 10.0.0](https://www.nuget.org/packages/Microsoft.AspNetCore.Components.WebAssembly.DevServer)** – development server for Blazor WASM
* **[PublicHoliday 3.9.0](https://www.nuget.org/packages/PublicHoliday/)** – used to determine public holiday exemptions

## License

This project is licensed under the **GNU General Public License v3.0 (GPLv3)**.  
For full license text, see the [LICENSE](./LICENSE) file.
