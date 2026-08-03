# ARC76 Account

.NET implementation of the [Algorand ARC76 Account Standard](https://github.com/algorandfoundation/ARCs/blob/main/ARCs/arc-0076.md):
deterministic, password-derived accounts, with one NuGet package and one namespace per
supported chain, so you only pull in the SDK/dependencies for the chains you actually use.

> Looking for the old, single `ARC76Account` package (v2.x, one assembly for every chain) or
> the pre-2.0 `AlgorandARC76Account` package / `AlgorandARC76AccountDotNet` namespace?
> See the [Migration guides](#migration-guides) below.

## Install

Each chain is its own package with its own dependencies — install only what you need:

```
dotnet add package ARC76Account.Algorand    # Algorand (AVM)
dotnet add package ARC76Account.Ethereum    # Ethereum / EVM-compatible chains
dotnet add package ARC76Account.Bitcoin     # Bitcoin
dotnet add package ARC76Account.BitcoinCash # Bitcoin Cash
```

`ARC76Account.Core` (the shared PBKDF2 seed derivation, no chain SDK dependencies) is pulled in
automatically by each of the above — install it directly only if you're implementing a new
chain yourself (see [Adding a new chain](#adding-a-new-chain)).

Installing `ARC76Account.Bitcoin` will not pull in `Algorand4`, `Nethereum.Signer`, or
`NBitcoin.Altcoins` — each package only depends on what its own chain needs.

Please tell your users to use a password of at least 50 characters long for best safety —
this library derives the entire account/private key deterministically from that password, so
its strength *is* the account's security.

## Supported chains

| Chain              | Package                       | Namespace                  | Type returned                  |
|--------------------|--------------------------------|------------------------------|-----------------------------------|
| Algorand (AVM)     | `ARC76Account.Algorand`        | `ARC76Account.Algorand`     | `Algorand.Algod.Model.Account`     |
| Ethereum / EVM     | `ARC76Account.Ethereum`        | `ARC76Account.Ethereum`     | `Nethereum.Signer.EthECKey`        |
| Bitcoin            | `ARC76Account.Bitcoin`         | `ARC76Account.Bitcoin`      | `NBitcoin.Key`                     |
| Bitcoin Cash       | `ARC76Account.BitcoinCash`     | `ARC76Account.BitcoinCash`  | `NBitcoin.Key`                     |

Every chain namespace exposes the same shape:

```c#
ARC76Account.<Chain>.ARC76.GetAccount(string password, int slot = 0);
ARC76Account.<Chain>.ARC76.GetEmailAccount(string email, string password, int slot = 0);
```

`slot` lets a single password derive multiple independent accounts on the same chain
(`slot: 0`, `slot: 1`, ... each produce a different account).

## Usage

### Algorand account from a password

```c#
Algorand.Algod.Model.Account account = ARC76Account.Algorand.ARC76.GetAccount(
    "12345678901234567890123456789012345678901234567890");
```

### Algorand account from an email + password

```c#
Algorand.Algod.Model.Account account = ARC76Account.Algorand.ARC76.GetEmailAccount(
    "email@example.com",
    "12345678901234567890123456789012345678901234567890");
```

### Multiple Algorand accounts from one password (slots)

```c#
var primary = ARC76Account.Algorand.ARC76.GetAccount(password);       // slot 0
var savings = ARC76Account.Algorand.ARC76.GetAccount(password, 1);    // slot 1
var trading = ARC76Account.Algorand.ARC76.GetAccount(password, 2);    // slot 2
```

### EVM (Ethereum & EVM-compatible chains) account from a password

```c#
var account = ARC76Account.Ethereum.ARC76.GetAccount(
    "12345678901234567890123456789012345678901234567890");
Assert.That(account.GetPublicAddress(), Is.EqualTo("0x6fD4983EaC3F958d1BF2009be4FCc0E8792A8AC6"));
Assert.That(account.GetPrivateKey(), Is.EqualTo("0x9365ff3d966fdb3b8765e27007677440e67382ed4503c2d00a39638548e13c22"));
```

### EVM account from an email + password

```c#
var account = ARC76Account.Ethereum.ARC76.GetEmailAccount(
    "email@example.com",
    "12345678901234567890123456789012345678901234567890");
Assert.That(account.GetPublicAddress(), Is.EqualTo("0x0b6CEF2E7844F27019Fce92205F5FedF684c6926"));
Assert.That(account.GetPrivateKey(), Is.EqualTo("0xe1078b9af5d2b58abc819135f6b8f358a3a36dd04d4707eb51f481d766a86915"));
```

### Bitcoin account from a password

Returns an `NBitcoin.Key`. The address uses native SegWit (BIP173 bech32, `bc1...`) — the
current standard for everyday Bitcoin addresses — and the private key uses WIF, the standard
Bitcoin private-key encoding:

```c#
using NBitcoin;

NBitcoin.Key key = ARC76Account.Bitcoin.ARC76.GetAccount(
    "12345678901234567890123456789012345678901234567890");
string address = key.PubKey.GetAddress(ScriptPubKeyType.Segwit, Network.Main).ToString();
string privateKeyWif = key.GetWif(Network.Main).ToString();
Assert.That(address, Is.EqualTo("bc1qudvfvw05l2a90kkl2xdjkh08egyt50ekkprkas"));
Assert.That(privateKeyWif, Is.EqualTo("L2AEXy6SztUrAa6bMZLyamdbF6xdALS6pUWBct6JKRLz9xDsm57Z"));
```

### Bitcoin account from an email + password

```c#
NBitcoin.Key key = ARC76Account.Bitcoin.ARC76.GetEmailAccount(
    "email@example.com",
    "12345678901234567890123456789012345678901234567890");
string address = key.PubKey.GetAddress(ScriptPubKeyType.Segwit, Network.Main).ToString();
Assert.That(address, Is.EqualTo("bc1qwjwpxtglzrv06ndfrqs2uegtmamg8qdzy3mghk"));
```

### Bitcoin Cash account from a password

Returns an `NBitcoin.Key` (Bitcoin Cash uses the same secp256k1 keys as Bitcoin — only the
address format and network parameters differ). The address uses **CashAddr**
(`bitcoincash:q...`), the standard Bitcoin Cash address format since the network-wide 2018
CashAddr migration. Bitcoin Cash does not support SegWit, so `ScriptPubKeyType.Legacy` here just
means "the chain's only P2PKH type", not a deprecated format:

```c#
using NBitcoin;
using NBitcoin.Altcoins;

NBitcoin.Key key = ARC76Account.BitcoinCash.ARC76.GetAccount(
    "12345678901234567890123456789012345678901234567890");
var network = BCash.Instance.Mainnet;
string address = key.PubKey.GetAddress(ScriptPubKeyType.Legacy, network).ToString();
string privateKeyWif = key.GetWif(network).ToString();
Assert.That(address, Is.EqualTo("bitcoincash:qr34393e7nat5476magek26aul9q3w3lxc4hph4ljz"));
Assert.That(privateKeyWif, Is.EqualTo("L2AEXy6SztUrAa6bMZLyamdbF6xdALS6pUWBct6JKRLz9xDsm57Z"));
```

### Bitcoin Cash account from an email + password

```c#
NBitcoin.Key key = ARC76Account.BitcoinCash.ARC76.GetEmailAccount(
    "email@example.com",
    "12345678901234567890123456789012345678901234567890");
string address = key.PubKey.GetAddress(ScriptPubKeyType.Legacy, NBitcoin.Altcoins.BCash.Instance.Mainnet).ToString();
Assert.That(address, Is.EqualTo("bitcoincash:qp6fcyedrugd3l2d4yvzptn9p00hdqup5g7zx5pytw"));
```

### Same password, two chains

Because the underlying seed derivation is identical, the same password/email/slot always
resolves to the same person's key material on every supported chain:

```c#
var algo = ARC76Account.Algorand.ARC76.GetAccount(password);
var evm  = ARC76Account.Ethereum.ARC76.GetAccount(password);
// algo.Address and evm.GetPublicAddress() are two different-chain accounts
// derived from the same password.
```

## ARC76Signer — ASP.NET Core transaction signer

Ships in `ARC76Account.Algorand` (no separate package needed). Configure your web API project
with an ARC76 signer configuration, and the signer will sign Algorand transactions whenever
required, without ever holding the raw password in your own code.

`appsettings.json`:
```json
{
  "ARC76Config": {
    "Data": "12345678901234567890123456789012345678901234567890"
  }
}
```

`Program.cs` / `Startup.cs`:
```c#
builder.Services.Configure<ARC76Account.Model.Config.ARC76Config>(
    builder.Configuration.GetSection("ARC76Config"));
builder.Services.AddSingleton<ARC76Account.Utils.ARC76Signer>();
```

`MyController.cs`:
```c#
public class MyController : ControllerBase
{
    private readonly ARC76Account.Utils.ARC76Signer signer;

    public MyController(ARC76Account.Utils.ARC76Signer signer)
    {
        this.signer = signer;
    }

    public void DoSomeAction()
    {
        var signed = signer.Sign(new Transaction[] { tx1, tx2 });
    }
}
```

You can also depend on `IAlgorandSigner` instead of the concrete `ARC76Signer` type to keep
your controllers testable/mockable:

```c#
builder.Services.AddSingleton<ARC76Account.Utils.IAlgorandSigner, ARC76Account.Utils.ARC76Signer>();
```

## Adding a new chain

The repo is organized as one **project/package** per chain (`ARC76Account.Algorand`,
`ARC76Account.Ethereum`, `ARC76Account.Bitcoin`, `ARC76Account.BitcoinCash`, ...), each with a
matching namespace and a static `ARC76` class exposing `GetAccount` / `GetEmailAccount`. All
chains reference `ARC76Account.Core` and call its public `Arc76Seed.Derive(...)` for the
shared PBKDF2 seed — that project has no chain SDK dependencies of its own. Adding a new chain
means adding one new project (folder + `.csproj` + `ARC76.cs`), not touching the others' code
or dependencies. See the `add-chain-support` Claude Code skill in this repo
(`.claude/skills/add-chain-support/SKILL.md`) for the step-by-step process.

## Migration guides

### v2.x → v3.0: one package per chain

Version 3.0 splits the single `ARC76Account` package into `ARC76Account.Core` plus one package
per chain. **Namespaces did not change** — only how you install the library did. This is a
breaking change for your `.csproj`/`packages.config`, not for your C# call sites.

```diff
- dotnet add package ARC76Account
+ dotnet add package ARC76Account.Algorand
+ dotnet add package ARC76Account.Ethereum
+ dotnet add package ARC76Account.Bitcoin
+ dotnet add package ARC76Account.BitcoinCash
```

Install only the chain packages you actually use — e.g. an Algorand-only project should add
just `ARC76Account.Algorand`, not all four. All your existing `ARC76Account.Algorand.ARC76.*`,
`ARC76Account.Ethereum.ARC76.*`, `ARC76Account.Utils.*`, etc. call sites are unchanged.

### v1.x → v2.0

Version 2.0 renames the project, NuGet package, and namespace, and reorganizes account
derivation into one namespace per chain. This is a breaking change — no compatibility shims
are provided, so update call sites when you upgrade.

**Package**

```diff
- dotnet add package AlgorandARC76Account
+ dotnet add package ARC76Account
```

**Namespace root**

```diff
- AlgorandARC76AccountDotNet
+ ARC76Account
```

**Algorand (AVM) account derivation**

```diff
- AlgorandARC76AccountDotNet.ARC76.GetAccount(password, slot)
+ ARC76Account.Algorand.ARC76.GetAccount(password, slot)

- AlgorandARC76AccountDotNet.ARC76.GetEmailAccount(email, password, slot)
+ ARC76Account.Algorand.ARC76.GetEmailAccount(email, password, slot)
```

**EVM account derivation**

```diff
- AlgorandARC76AccountDotNet.ARC76.GetEVMAccount(password, slot)
+ ARC76Account.Ethereum.ARC76.GetAccount(password, slot)

- AlgorandARC76AccountDotNet.ARC76.GetEVMEmailAccount(email, password, slot)
+ ARC76Account.Ethereum.ARC76.GetEmailAccount(email, password, slot)
```

**Signer & config**

```diff
- AlgorandARC76AccountDotNet.Utils.ARC76Signer
+ ARC76Account.Utils.ARC76Signer

- AlgorandARC76AccountDotNet.Utils.IAlgorandSigner
+ ARC76Account.Utils.IAlgorandSigner

- AlgorandARC76AccountDotNet.Model.Config.ARC76Config
+ ARC76Account.Model.Config.ARC76Config
```

Nothing about the derivation algorithm itself changed (same PBKDF2-HMAC-SHA256 parameters, same
salt/input format) — a given password/email/slot still derives the exact same keys on every
chain as it did under the old package. Only the package name, root namespace, and method
locations changed.

## AI-assisted development

If you're using an AI coding assistant (Claude Code, GitHub Copilot, Cursor, etc.) to write
code against this library, see [AI_INTEGRATION.md](AI_INTEGRATION.md) for a concise,
LLM-friendly reference of the current API surface, common patterns, and pitfalls to avoid.