import { deriveSeed, deriveSeedWithEmail } from "./index";

const PASSWORD = "12345678901234567890123456789012345678901234567890";
const EMAIL = "email@example.com";

describe("deriveSeed", () => {
  it("returns a deterministic 32-byte seed", async () => {
    const a = await deriveSeed(PASSWORD);
    const b = await deriveSeed(PASSWORD);
    expect(a.length).toBe(32);
    expect(Buffer.from(a)).toEqual(Buffer.from(b));
  });

  it("differs per slot", async () => {
    const slot0 = await deriveSeed(PASSWORD, 0);
    const slot1 = await deriveSeed(PASSWORD, 1);
    expect(Buffer.from(slot0)).not.toEqual(Buffer.from(slot1));
  });

  it("rejects short passwords", async () => {
    await expect(deriveSeed("short")).rejects.toThrow();
  });
});

describe("deriveSeedWithEmail", () => {
  it("differs from the password-only seed", async () => {
    const withoutEmail = await deriveSeed(PASSWORD);
    const withEmail = await deriveSeedWithEmail(EMAIL, PASSWORD);
    expect(Buffer.from(withoutEmail)).not.toEqual(Buffer.from(withEmail));
  });

  it("is deterministic for the same email/password/slot", async () => {
    const a = await deriveSeedWithEmail(EMAIL, PASSWORD, 0);
    const b = await deriveSeedWithEmail(EMAIL, PASSWORD, 0);
    expect(Buffer.from(a)).toEqual(Buffer.from(b));
  });
});
