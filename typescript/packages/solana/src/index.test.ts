import bs58 from "bs58";
import { getAccount, getEmailAccount } from "./index";

const PASSWORD = "12345678901234567890123456789012345678901234567890";
const EMAIL = "email@example.com";

// Known-answer values shared with dotnet/TestARC76/SolanaTests.cs - if these ever diverge,
// cross-language ARC76 compatibility has broken.
describe("getAccount", () => {
  it("matches the known password-derived address and private key", async () => {
    const account = await getAccount(PASSWORD);
    expect(bs58.encode(account.publicKey)).toBe("4NZNrQAS9gLtqcutnCRfyy63M7XysEzgPP4hFGrutsne");
    expect(bs58.encode(account.secretKey)).toBe(
      "3wvbZFR7njig8pAcSaHtzAVXYe779HjwrxCYiXRJS7shfWV6N3bNNynRijtz5WccRfDfhcHF5VTyq8yCv5HMAf34"
    );
  });

  // Regression test for a slot!=0 known-answer value - see audits/2026-08-03-audit-report.md
  // (Medium finding: no test previously exercised any slot other than the default).
  it("matches the known slot-1 address and private key", async () => {
    const account = await getAccount(PASSWORD, 1);
    expect(bs58.encode(account.publicKey)).toBe("D9cyFwbaGdDd1Lo3MJbL6VULN8r4p3c2jrWURTUV7kB");
    expect(bs58.encode(account.secretKey)).toBe(
      "2WT1aN7vJse9uykpD6kgZs5zUovYKUxDKDEswPwEa9JnLgrEKseRQpxatFXfDHs9UohZB9A7qGRRZdBGzkWSi8K9"
    );
  });
});

describe("getEmailAccount", () => {
  it("matches the known email+password-derived address and private key", async () => {
    const account = await getEmailAccount(EMAIL, PASSWORD);
    expect(bs58.encode(account.publicKey)).toBe("GcsDHS3b2hSUAWb2sU3UYvNWjudn4zas2h969q8C6P8r");
    expect(bs58.encode(account.secretKey)).toBe(
      "5Vwqas2RYTc5iq4eN4cvx79AfZPY7ygvLt2AtQ5eMdzT9ZgeLDu57m2tpnyw7Z53TtZknyEs1qUMvfwhUkVeDLhC"
    );
  });
});
