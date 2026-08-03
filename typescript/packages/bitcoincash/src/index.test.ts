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
