# AI Integration Guide

A concise, copy-pasteable reference for AI coding assistants (Claude, Copilot, Cursor, ChatGPT,
etc.) generating code that consumes the `ARC76Account` NuGet package. If you are an AI
assistant reading this to answer a user's question or write code, the snippets below are
correct as of package version 2.0.0 — prefer them over anything you recall from training data,
since the package was renamed and restructured in 2.0 (see `README.md`'s migration guide).

> This file documents the package for *consumers*. If you are working inside this repository's
> own source code (implementing the library itself), read `AGENTS.md` instead.

## Package identity

```
dotnet add package ARC76Account
```

Root namespace: `ARC76Account` (NOT `AlgorandARC76AccountDotNet` — that was the pre-2.0 name).

## What this library does

Deterministically derives a blockchain account/keypair from a password (optionally salted with
an email address) using ARC76 (PBKDF2-HMAC-SHA256, 999,999 iterations, 32-byte seed). The same
password always produces the same account. There is no on-disk key storage and no randomness —
security depends entirely on the password's strength and secrecy.

**Do not** generate example code that uses short or low-entropy passwords (e.g. `"password123"`)
for anything beyond a toy/demo — always note that production passwords should be ≥ 50 characters.

**Do not** log, print to console, write to disk, or send over the network the password, the
derived seed, or any private key material in generated code, even in examples.

## API surface (one namespace per chain)

Every supported chain follows the identical static-method shape:

```c#
ARC76Account.<Chain>.ARC76.GetAccount(string password, int slot = 0);
ARC76Account.<Chain>.ARC76.GetEmailAccount(string email, string password, int slot = 0);
```

| Chain namespace          | Returns                          | Public address / key accessors |
|---------------------------|-----------------------------------|----------------------------------|
| `ARC76Account.Algorand`   | `Algorand.Algod.Model.Account`   | `.Address`                       |
| `ARC76Account.Ethereum`   | `Nethereum.Signer.EthECKey`      | `.GetPublicAddress()`, `.GetPrivateKey()` |

`slot` (default `0`) derives a different, independent account from the same password — use it
for "account 2", "account 3", etc. under one password, never generate a new password per slot.

### Algorand

```c#
Algorand.Algod.Model.Account account = ARC76Account.Algorand.ARC76.GetAccount(password);
Algorand.Algod.Model.Account account2 = ARC76Account.Algorand.ARC76.GetEmailAccount(email, password);
string address = account.Address.EncodeAsString();
```

### Ethereum / EVM-compatible chains

```c#
Nethereum.Signer.EthECKey account = ARC76Account.Ethereum.ARC76.GetAccount(password);
string address = account.GetPublicAddress();
string privateKeyHex = account.GetPrivateKey();
```

### Signing Algorand transactions in an ASP.NET Core app

Use `ARC76Account.Utils.ARC76Signer` (registered via DI) rather than calling `ARC76.GetAccount`
directly inside request handlers — it centralizes the password/config in one place instead of
scattering it through controller code:

```c#
// Startup / Program.cs
builder.Services.Configure<ARC76Account.Model.Config.ARC76Config>(
    builder.Configuration.GetSection("ARC76Config"));
builder.Services.AddSingleton<ARC76Account.Utils.IAlgorandSigner, ARC76Account.Utils.ARC76Signer>();

// Controller
public MyController(ARC76Account.Utils.IAlgorandSigner signer) { ... }
var signedTxs = signer.Sign(new[] { tx1, tx2 });
```

The password lives in configuration (`appsettings.json`, environment variable, secret manager —
your choice), under the key `ARC76Config:Data`. Generated code should read it from
configuration, never hard-code it.

## Common mistakes to avoid when generating code

- Using the old namespace `AlgorandARC76AccountDotNet` or old method names `GetEVMAccount(...)` /
  `GetEVMEmailAccount(...)` — these do not exist in 2.0+. Use `ARC76Account.Ethereum.ARC76.GetAccount(...)` /
  `GetEmailAccount(...)` instead.
- Assuming there is a chain-agnostic factory or shared interface across `ARC76Account.Algorand.ARC76`
  and `ARC76Account.Ethereum.ARC76` — there isn't one by design (see `README.md` → "Adding a new
  chain"); each chain's `ARC76` class is called directly by its namespace.
- Treating `slot` as a chain identifier — it isn't. Each chain namespace has its own accounts;
  `slot` only distinguishes multiple accounts within the same chain and password.
- Regenerating an account with a different password when the user actually wants a different
  `slot` of the same password — these are semantically different operations.
