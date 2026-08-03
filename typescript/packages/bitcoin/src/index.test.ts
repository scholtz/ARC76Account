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
});

describe("getEmailAccount", () => {
  it("matches the known email+password-derived address and WIF", async () => {
    const account = await getEmailAccount(EMAIL, PASSWORD);
    expect(addressOf(account.publicKey)).toBe("bc1qwjwpxtglzrv06ndfrqs2uegtmamg8qdzy3mghk");
    expect(account.toWIF()).toBe("L4m915x48Hasve5Dzc2BnJuUaxX85xWeF8PtUo5dP234VtSZjDtL");
  });
});
