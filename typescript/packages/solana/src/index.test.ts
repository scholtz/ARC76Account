import { readFileSync } from "fs";
import { join } from "path";
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

// Extends the literal tests above with the cross-language vectors in test-vectors/arc76-vectors.json
// (repo root, shared with dotnet/ and python/) - if the languages diverge, cross-language ARC76
// compatibility has broken.
interface SharedVector {
  email: string | null;
  password: string;
  slot: number;
  [field: string]: string | number | null;
}
const VECTORS = JSON.parse(
  readFileSync(join(__dirname, "../../../../test-vectors/arc76-vectors.json"), "utf8")
) as { seeds: SharedVector[]; accounts: Record<string, SharedVector[]> };
const label = (v: SharedVector) =>
  `${v.email ?? "(no email)"} / slot ${v.slot} / ${v.password.slice(0, 8)}`;

describe("shared vectors", () => {
  it.each(VECTORS.accounts.solana.map((v) => [label(v), v] as const))("%s", async (_l, v) => {
    const account =
      v.email === null
        ? await getAccount(v.password, v.slot)
        : await getEmailAccount(v.email, v.password, v.slot);
    expect(bs58.encode(account.publicKey)).toBe(v.publicKey);
    expect(bs58.encode(account.secretKey)).toBe(v.secretKey);
  });
});
