using System.Text.Json;

namespace TestARC76
{
    /// <summary>
    /// Loads the cross-language known-answer vectors from test-vectors/arc76-vectors.json (repo
    /// root, copied next to the test assembly). The same file drives the Python and TypeScript
    /// test suites - never hard-code known-answer values in a single language's tests.
    /// </summary>
    public sealed record Vector(string? Email, string Password, int Slot, JsonElement Raw)
    {
        public string Str(string field) => Raw.GetProperty(field).GetString()!;
        public override string ToString() => $"{Email ?? "(no email)"} / slot {Slot} / {Password[..Math.Min(8, Password.Length)]}...";
    }

    public static class Vectors
    {
        private static readonly JsonDocument Doc = JsonDocument.Parse(
            File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "arc76-vectors.json")));

        private static IEnumerable<Vector> Parse(JsonElement array) =>
            array.EnumerateArray().Select(e => new Vector(
                e.GetProperty("email").GetString(),
                e.GetProperty("password").GetString()!,
                e.GetProperty("slot").GetInt32(),
                e)).ToList();

        public static IEnumerable<Vector> Seeds() => Parse(Doc.RootElement.GetProperty("seeds"));

        public static IEnumerable<Vector> Accounts(string chain) =>
            Parse(Doc.RootElement.GetProperty("accounts").GetProperty(chain));

        public static IEnumerable<TestCaseData> SeedCases() => Seeds().Select(v => new TestCaseData(v).SetArgDisplayNames(v.ToString()));
        public static IEnumerable<TestCaseData> AlgorandCases() => Accounts("algorand").Select(v => new TestCaseData(v).SetArgDisplayNames(v.ToString()));
        public static IEnumerable<TestCaseData> FalconCases() => Accounts("falcon").Select(v => new TestCaseData(v).SetArgDisplayNames(v.ToString()));
        public static IEnumerable<TestCaseData> EthereumCases() => Accounts("ethereum").Select(v => new TestCaseData(v).SetArgDisplayNames(v.ToString()));
        public static IEnumerable<TestCaseData> BitcoinCases() => Accounts("bitcoin").Select(v => new TestCaseData(v).SetArgDisplayNames(v.ToString()));
        public static IEnumerable<TestCaseData> BitcoinCashCases() => Accounts("bitcoincash").Select(v => new TestCaseData(v).SetArgDisplayNames(v.ToString()));
        public static IEnumerable<TestCaseData> SolanaCases() => Accounts("solana").Select(v => new TestCaseData(v).SetArgDisplayNames(v.ToString()));
    }
}
