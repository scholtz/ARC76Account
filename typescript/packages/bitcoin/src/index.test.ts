import { readFileSync } from "fs";
import { join } from "path";
import * as bitcoin from "bitcoinjs-lib";
import { getAccount, getEmailAccount } from "./index";

const PASSWORD = "12345678901234567890123456789012345678901234567890";
const EMAIL = "email@example.com";

function addressOf(publicKey: Uint8Array): string {
  const { address } = bitcoin.payments.p2wpkh({ pubkey: Buffer.from(publicKey) });
  if (!address) throw new Error("failed to derive address");
  return address;
}

// Known-answer values shared with dotnet/TestARC76/BitcoinTests.cs - if these ever diverge,
// cross-language ARC76 compatibility has broken.
describe("getAccount", () => {
  it("matches the known password-derived address and WIF", async () => {
    const account = await getAccount(PASSWORD);
    expect(addressOf(account.publicKey)).toBe("bc1qudvfvw05l2a90kkl2xdjkh08egyt50ekkprkas");
    expect(account.toWIF()).toBe("L2AEXy6SztUrAa6bMZLyamdbF6xdALS6pUWBct6JKRLz9xDsm57Z");
  });

  // Regression test for a slot!=0 known-answer value - see audits/2026-08-03-audit-report.md
  // (Medium finding: no test previously exercised any slot other than the default).
  it("matches the known slot-1 address and WIF", async () => {
    const account = await getAccount(PASSWORD, 1);
    expect(addressOf(account.publicKey)).toBe("bc1q0l7cwnvtfwt538vv9yreuyrxuamgl470fkl8j0");
    expect(account.toWIF()).toBe("KykJVUYMpC2xCrUb8dENcRP3cvQHt6BQCskrkJvyW7fvGEVxpgWA");
  });
});

describe("getEmailAccount", () => {
  it("matches the known email+password-derived address and WIF", async () => {
    const account = await getEmailAccount(EMAIL, PASSWORD);
    expect(addressOf(account.publicKey)).toBe("bc1qwjwpxtglzrv06ndfrqs2uegtmamg8qdzy3mghk");
    expect(account.toWIF()).toBe("L4m915x48Hasve5Dzc2BnJuUaxX85xWeF8PtUo5dP234VtSZjDtL");
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
  it.each(VECTORS.accounts.bitcoin.map((v) => [label(v), v] as const))("%s", async (_l, v) => {
    const account =
      v.email === null
        ? await getAccount(v.password, v.slot)
        : await getEmailAccount(v.email, v.password, v.slot);
    expect(addressOf(account.publicKey)).toBe(v.address);
    expect(account.toWIF()).toBe(v.wif);
  });
});
