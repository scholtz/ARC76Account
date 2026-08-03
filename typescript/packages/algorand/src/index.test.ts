import { encodeAddress } from "algosdk";
import { getAccount, getEmailAccount } from "./index";

const PASSWORD = "12345678901234567890123456789012345678901234567890";
const EMAIL = "email@example.com";

// Known-answer values shared with dotnet/TestARC76/AVMTests.cs - if these ever diverge,
// cross-language ARC76 compatibility has broken.
describe("getAccount", () => {
  it("matches the known password-derived address", async () => {
    const account = await getAccount(PASSWORD);
    expect(encodeAddress(account.addr.publicKey)).toBe(
      "GIMN3RFDRRWFD7KIFSVRMXC4JXYD5ZXKT7KSN57OG4RAZPPC3L3YDM46NQ"
    );
  });
});

describe("getEmailAccount", () => {
  it("matches the known email+password-derived address", async () => {
    const account = await getEmailAccount(EMAIL, PASSWORD);
    expect(encodeAddress(account.addr.publicKey)).toBe(
      "5AHWQJ5D52K4GRW4JWQ5GMR53F7PDSJEGT4PXVFSBQYE7VXDVG3WSPWSBM"
    );
  });
});
