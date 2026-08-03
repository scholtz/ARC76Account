import algosdk from "algosdk";
import { generateAlgorandAccount } from "./index";
/**
 * @jest-environment jsdom
 */
test("generateAlgorandAccount should return correct address", async () => {
  const account = await generateAlgorandAccount(
    "12345678901234567890123456789012345678901234567890",
    "email@example.com",
    0
  );
  expect(algosdk.encodeAddress(account.addr.publicKey)).toBe(
    "5AHWQJ5D52K4GRW4JWQ5GMR53F7PDSJEGT4PXVFSBQYE7VXDVG3WSPWSBM"
  );
});
