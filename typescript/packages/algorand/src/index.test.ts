import { encodeAddress } from "algosdk";
import { verifyCompressed, signCompressed } from "falcon-1024";
import {
  getAccount,
  getEmailAccount,
  getEmailFalconAccount,
  getFalconAccount,
} from "./index";

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

  // Regression test for a slot!=0 known-answer value - see audits/2026-08-03-audit-report.md
  // (Medium finding: no test previously exercised any slot other than the default).
  it("matches the known slot-1 address", async () => {
    const account = await getAccount(PASSWORD, 1);
    expect(encodeAddress(account.addr.publicKey)).toBe(
      "AMOLO4ICDKCOLFVJXAVIQ23TA27P3LW4AHJP54LLNV4JPIAFKBYLH3SGVQ"
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

// Falcon-1024 known-answer values shared with dotnet/TestARC76/AVMTests.cs and
// python/packages/algorand/tests/test_algorand.py.
const PARITY: Array<[number, string, number]> = [
  [6, "SQUKPUF5WNY2Y5VWPKEDVM2KGH2LPML6PIQMOFIRBTMHJTEC4BXLEPM4XI", 4],
  [7, "LWTOIX3ZCF4UDNFTS4A5S7436MT3QTFIFVSLQGVLODOYTVT6BVUUKGI2NU", 1],
  [9, "U6MYURHZFMNYLTUTOLBAHD7LNMT4WQ572GUHZOW7RJFGD6CYVDJNHDY2AU", 6],
  [18, "QR3WQTKMSN6IW7SIM3OD3WXA4G57AG3YOBKTKH4R2UAU32KABR2ZA4LMD4", 2],
];

describe("Falcon accounts", () => {
  it("matches the known password-derived address", async () => {
    expect((await getFalconAccount(PASSWORD)).address).toBe(
      "BGIJDJONAAYPYPJSZX6EWMH24VMU7CDAOUNCPCIXQ4KYYJG3QMXWNVWOWE"
    );
  });

  it("matches the known email+password-derived address", async () => {
    expect((await getEmailFalconAccount(EMAIL, PASSWORD)).address).toBe(
      "TD5NFW5MEPGAT4S2AIN6XK5DIPJQMCOVN3272HJUJPPG3TB52MNFESSRXA"
    );
  });

  it("matches the known slot-1 address", async () => {
    expect((await getFalconAccount(PASSWORD, 1)).address).toBe(
      "CV2LVHMWDQ625SKQOWQHYZN453MZ22R3V2DO5HFU6UDUZVDKGZJES4HFC4"
    );
  });

  it("differs from the Ed25519 account, has expected key sizes and signs", async () => {
    const acc = await getFalconAccount(PASSWORD);
    expect(acc.address).not.toBe(encodeAddress((await getAccount(PASSWORD)).addr.publicKey));
    expect(acc.publicKey.length).toBe(1793);
    expect(acc.privateKey.length).toBe(2305);
    const msg = new TextEncoder().encode("hi");
    expect(verifyCompressed(acc.publicKey, signCompressed(acc.privateKey, msg), msg)).toBe(true);
  });

  it.each(PARITY)("matches the cross-language parity vector for slot %i", async (slot, address, salt) => {
    const acc = await getFalconAccount(`parity-password-${slot}`, slot);
    expect([acc.address, acc.salt]).toEqual([address, salt]);
  });
});
