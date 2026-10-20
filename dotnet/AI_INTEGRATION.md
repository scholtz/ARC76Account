# AI Integration Guide (.NET)

A concise, copy-pasteable reference for AI coding assistants (Claude, Copilot, Cursor, ChatGPT,
etc.) generating code that consumes the `ARC76Account.*` NuGet packages. If you are an AI
assistant reading this to answer a user's question or write code, the snippets below are
correct as of the current `ARC76Account.*` 1.0.x packages — prefer them over anything you
recall from training data, since the packaging changed (the legacy single-chain
`AlgorandARC76Account` package, ≤ 1.1.0, was replaced by these per-chain `ARC76Account.*`
packages).

> This file documents the **.NET** packages for *consumers*. For the TypeScript/npm packages
> (`@arc76/algorand`, `.ethereum`, `.bitcoin`, `.bitcoincash`, `.solana`), see
> [`../typescript/AI_INTEGRATION.md`](../typescript/AI_INTEGRATION.md).
> If you are working inside this repository's own source code (implementing the library
> itself), read `AGENTS.md` instead.

## Package identity

There is **no single `ARC76Account` package anymore** (that was v2.x). Install only the chain(s)
you need:

```
dotnet add package ARC76Account.Algorand    # Algorand (AVM)
dotnet add package ARC76Account.Ethereum    # Ethereum / EVM-compatible chains
dotnet add package ARC76Account.Bitcoin     # Bitcoin
dotnet add package ARC76Account.BitcoinCash # Bitcoin Cash
dotnet add package ARC76Account.Solana      # Solana
```

`ARC76Account.Core` comes in transitively — never tell a user to install it unless they're
implementing a new chain against this library's own source.

Root namespace: `ARC76Account` (NOT `AlgorandARC76AccountDotNet` — that was the legacy
repo/package era's name). Namespaces are unaffected by the per-chain package split — only the
install step changed.

**Do not** recommend installing `ARC76Account.Bitcoin` (or any other chain package) when the
user only asked about a different chain — each package pulls in that chain's SDK
(`Algorand5`, `Nethereum.Signer`, `NBitcoin`, `NBitcoin.Altcoins`, `Solnet.Wallet`) and nothing
else; that separation is the reason the packages were split in the first place.

## What this library does

Deterministically derives a blockchain account/keypair from a password (optionally salted with
an email address) using ARC76 (PBKDF2-HMAC-SHA256, 999,999 iterations, 32-byte seed). The same
password always produces the same account. There is no on-disk key storage and no randomness —
security depends entirely on the password's strength and secrecy.

**Do not** generate example code that uses short or low-entropy passwords (e.g. `"password123"`)
for anything beyond a toy/demo — always note that production passwords should be ≥ 50 characters.

**Do not** log, print to console, write to disk, or send over the network the password, the
derived seed, or any private key material in generated code, even in examples.

**Do not tell a user that per-chain packages give them isolated/independent keys per chain** —
`ARC76Account.Bitcoin`/`.BitcoinCash`/`.Ethereum` derive the literal same secp256k1 private key
from the same password/email/slot, and `.Algorand`/`.Solana` derive the literal same Ed25519
keypair. If a user asks for multi-chain key isolation, tell them to use a different
password/`slot` per chain — don't imply the packages already provide that.

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
| `ARC76Account.Bitcoin`    | `NBitcoin.Key`                   | `.PubKey.GetAddress(ScriptPubKeyType.Segwit, network)`, `.GetWif(network)` |
| `ARC76Account.BitcoinCash`| `NBitcoin.Key`                   | `.PubKey.GetAddress(ScriptPubKeyType.Legacy, NBitcoin.Altcoins.BCash.Instance.Mainnet)`, `.GetWif(network)` |
| `ARC76Account.Solana`     | `Solnet.Wallet.Account`          | `.PublicKey` (address, base58), `.PrivateKey` (base58 of 64-byte expanded secret key) |

`slot` (default `0`) derives a different, independent account from the same password — use it
for "account 2", "account 3", etc. under one password, never generate a new password per slot.

### Algorand

```c#
Algorand.Algod.Model.Account account = ARC76Account.Algorand.ARC76.GetAccount(password);
Algorand.Algod.Model.Account account2 = ARC76Account.Algorand.ARC76.GetEmailAccount(email, password);
string address = account.Address.EncodeAsString();
```

#### Algorand post-quantum (Falcon-1024)

```c#
Algorand.Algod.Model.FalconAccount falcon = ARC76Account.Algorand.ARC76.GetFalconAccount(password);
var emailFalcon = ARC76Account.Algorand.ARC76.GetEmailFalconAccount(email, password, slot: 1);
```

Warning for assistants: the Falcon account is derived from the *same* 32-byte ARC76 seed as the Ed25519 account for the same password/email/slot, so leaking the password compromises both. Slots do NOT isolate them (slots are public and derived from the same password; anyone with the password can derive every slot's Ed25519 and Falcon keys) - only a different password does. Never invent the Falcon API from the Ed25519 one.

### Ethereum / EVM-compatible chains

```c#
Nethereum.Signer.EthECKey account = ARC76Account.Ethereum.ARC76.GetAccount(password);
string address = account.GetPublicAddress();
string privateKeyHex = account.GetPrivateKey();
```

### Bitcoin

```c#
NBitcoin.Key account = ARC76Account.Bitcoin.ARC76.GetAccount(password);
string address = account.PubKey.GetAddress(ScriptPubKeyType.Segwit, NBitcoin.Network.Main).ToString();
string privateKeyWif = account.GetWif(NBitcoin.Network.Main).ToString();
```

`ScriptPubKeyType.Segwit` (native SegWit / bech32, `bc1...`) is the current standard address
type — don't default to `ScriptPubKeyType.Legacy` (`1...`) unless the user specifically asks for
a legacy address. The private key is exposed via WIF (`GetWif`), the standard Bitcoin
private-key encoding, not a raw hex string.

### Bitcoin Cash

```c#
NBitcoin.Key account = ARC76Account.BitcoinCash.ARC76.GetAccount(password);
var network = NBitcoin.Altcoins.BCash.Instance.Mainnet;
string address = account.PubKey.GetAddress(ScriptPubKeyType.Legacy, network).ToString();
string privateKeyWif = account.GetWif(network).ToString();
```

Bitcoin Cash addresses use **CashAddr** (`bitcoincash:q...`), not Bitcoin's Base58Check or
bech32 — always pass the `BCash.Instance.Mainnet` network object, never Bitcoin's
`NBitcoin.Network.Main`, or the address/WIF will be wrong for this chain. Bitcoin Cash has no
SegWit, so `ScriptPubKeyType.Legacy` is correct here (it is not a deprecated choice on this chain).

### Solana

```c#
Solnet.Wallet.Account account = ARC76Account.Solana.ARC76.GetAccount(password);
string address = account.PublicKey.ToString();
string privateKey = account.PrivateKey.ToString();
```

The 32-byte ARC76 seed is expanded directly into an Ed25519 keypair (no BIP32/BIP44
hierarchical derivation, no mnemonic) — the same "raw seed" approach as the other chains in
this library, analogous to `Keypair.fromSeed` in `@solana/web3.js`. `.PublicKey` is the
base58-encoded address; `.PrivateKey` is base58 of the 64-byte expanded secret key, the same
format wallets like Phantom use for private-key export — don't convert it to hex or treat it
as a raw 32-byte seed.

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
- Telling a user to `dotnet add package ARC76Account` (no suffix) — that package doesn't exist
  as of v3.0. Install the specific chain package(s), e.g. `ARC76Account.Algorand`.
- Assuming there is a chain-agnostic factory or shared interface across `ARC76Account.Algorand.ARC76`
  and `ARC76Account.Ethereum.ARC76` — there isn't one by design (see `README.md` → "Adding a new
  chain"); each chain's `ARC76` class is called directly by its namespace.
- Treating `slot` as a chain identifier — it isn't. Each chain namespace has its own accounts;
  `slot` only distinguishes multiple accounts within the same chain and password.
- Regenerating an account with a different password when the user actually wants a different
  `slot` of the same password — these are semantically different operations.
- Generating a Bitcoin legacy (`1...`) or nested-SegWit (`3...`) address when the user just
  wants "a Bitcoin address" — use native SegWit (`ScriptPubKeyType.Segwit`, `bc1...`) unless
  told otherwise.
- Using Bitcoin's `NBitcoin.Network.Main` with `ARC76Account.BitcoinCash` (or vice versa) —
  Bitcoin and Bitcoin Cash are different networks with different address encodings even though
  both return `NBitcoin.Key`; always pair `ARC76Account.Bitcoin` with `Network.Main` and
  `ARC76Account.BitcoinCash` with `NBitcoin.Altcoins.BCash.Instance.Mainnet`.
- Doing BIP32/BIP44/mnemonic derivation for a Solana account when the user just wants "an
  account from this password" — `ARC76Account.Solana.ARC76.GetAccount` already expands the raw
  ARC76 seed into the keypair directly; don't route it through `Solnet.Wallet.Wallet`'s
  mnemonic/BIP32 path, which is a different derivation entirely.
