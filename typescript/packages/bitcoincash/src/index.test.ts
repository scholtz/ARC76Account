import { readFileSync } from "fs";
import { join } from "path";
import * as bitcoin from "bitcoinjs-lib";
import { encode } from "cashaddrjs";
import { getAccount, getEmailAccount } from "./index";

const PASSWORD = "12345678901234567890123456789012345678901234567890";
const EMAIL = "email@example.com";

function addressOf(publicKey: Uint8Array): string {
  const hash160 = bitcoin.crypto.hash160(Buffer.from(publicKey));
  return encode("bitcoincash", "P2PKH", hash160);
}

// Known-answer values shared with dotnet/TestARC76/BitcoinCashTests.cs - if these ever
// diverge, cross-language ARC76 compatibility has broken.
describe("getAccount", () => {
  it("matches the known password-derived address and WIF", async () => {
    const account = await getAccount(PASSWORD);
    expect(addressOf(account.publicKey)).toBe(
      "bitcoincash:qr34393e7nat5476magek26aul9q3w3lxc4hph4ljz"
    );
    expect(account.toWIF()).toBe("L2AEXy6SztUrAa6bMZLyamdbF6xdALS6pUWBct6JKRLz9xDsm57Z");
  });

  // Regression test for a slot!=0 known-answer value - see audits/2026-08-03-audit-report.md
  // (Medium finding: no test previously exercised any slot other than the default).
  it("matches the known slot-1 address and WIF", async () => {
    const account = await getAccount(PASSWORD, 1);
    expect(addressOf(account.publicKey)).toBe(
      "bitcoincash:qpllmp6d3d9ewjya3s5s08ssvmnhdr7heuv0dvkuds"
    );
    expect(account.toWIF()).toBe("KykJVUYMpC2xCrUb8dENcRP3cvQHt6BQCskrkJvyW7fvGEVxpgWA");
  });
});

describe("getEmailAccount", () => {
  it("matches the known email+password-derived address and WIF", async () => {
    const account = await getEmailAccount(EMAIL, PASSWORD);
    expect(addressOf(account.publicKey)).toBe(
      "bitcoincash:qp6fcyedrugd3l2d4yvzptn9p00hdqup5g7zx5pytw"
    );
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
  it.each(VECTORS.accounts.bitcoincash.map((v) => [label(v), v] as const))("%s", async (_l, v) => {
    const account =
      v.email === null
        ? await getAccount(v.password, v.slot)
        : await getEmailAccount(v.email, v.password, v.slot);
    expect(addressOf(account.publicKey)).toBe(v.address);
    expect(account.toWIF()).toBe(v.wif);
  });
});
