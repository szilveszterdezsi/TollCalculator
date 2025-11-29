using System.Net.Http.Json;
using TollCalculator.Models;

namespace TollCalculator.Services;

public class RulesRepository(HttpClient httpClient)
{
    private const string RULES_ENDPOINT = "data/rules.json";
    public async Task<Rules> GetRulesAsync()
        => await httpClient.GetFromJsonAsync<Rules>(RULES_ENDPOINT) ?? new();
}