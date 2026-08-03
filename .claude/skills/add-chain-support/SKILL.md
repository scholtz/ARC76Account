---
name: add-chain-support
description: Use when asked to add ARC76 account derivation support for a new blockchain/chain (e.g. TON, Polkadot, another EVM-compatible variant) to the ARC76Account library. Walks through the new project, package, and test files that must be created, following the existing per-chain-package pattern (ARC76Account.Algorand, ARC76Account.Ethereum, ARC76Account.Bitcoin, ARC76Account.BitcoinCash, ARC76Account.Solana).
---

# Add chain support to ARC76Account (.NET)

This is for the **.NET** implementation only, under `dotnet/` in this repo (a multi-language
monorepo — see the repo-root `AGENTS.md`). All paths below are relative to `dotnet/`.

This library derives deterministic accounts for multiple chains from the same ARC76 seed
(PBKDF2-HMAC-SHA256, shared in `ARC76Account.Core/Arc76Seed.cs`, public method
`Arc76Seed.Derive(...)`). Since v3.0, **each chain is its own .NET project and NuGet package**
(`ARC76Account.Algorand`, `ARC76Account.Ethereum`, `ARC76Account.Bitcoin`,
`ARC76Account.BitcoinCash`, `ARC76Account.Solana`), each with a static `ARC76` class exposing
`GetAccount`/`GetEmailAccount`, and each depending only on `ARC76Account.Core` plus its own
chain SDK. Adding a new chain means adding one new project, not a new file in an existing one —
never invent a new derivation scheme, never duplicate the PBKDF2 logic, and never add another
chain's SDK dependency to an unrelated project.

If instead asked to add chain support to the TypeScript package (`typescript/`), this skill
doesn't apply as-is — that package currently has a single-chain (Algorand-only), single-file
structure; see `typescript/AGENTS.md`.

## Steps

All commands below are run from `dotnet/`.

1. **Find the chain's key-from-seed constructor.** Look for a minimal signer package
   (analogous to `Nethereum.Signer.EthECKey` for EVM, or `NBitcoin.Key` for Bitcoin) that can
   build a keypair directly from a 32-byte seed/private key, without pulling in a full node SDK.

2. **Create the new project** `ARC76Account.<Chain>/ARC76Account.<Chain>.csproj` (i.e.
   `dotnet/ARC76Account.<Chain>/...`), following `ARC76Account.Ethereum/ARC76Account.Ethereum.csproj`
   as the template:
   ```xml
   <Project Sdk="Microsoft.NET.Sdk">

       <PropertyGroup>
           <Title>ARC76 Account - <Chain></Title>
           <Description>Deterministic ARC76 account derivation for <Chain>.</Description>
           <PackageTags><Chain>;ARC76;Account generation</PackageTags>
           <PackageId>ARC76Account.<Chain></PackageId>
       </PropertyGroup>

       <ItemGroup>
           <PackageReference Include="<the chain's key package>" Version="<latest>" />
       </ItemGroup>

       <ItemGroup>
           <ProjectReference Include="..\ARC76Account.Core\ARC76Account.Core.csproj" />
       </ItemGroup>

   </Project>
   ```
   Don't repeat `TargetFramework`/`Nullable`/`ImplicitUsings`/`Authors`/`PackageProjectUrl`/etc.
   — those come from the root `Directory.Build.props`, which every project in the repo already
   inherits. Only set what's specific to this chain package.

3. **Add the project to the solution**: `dotnet sln add ARC76Account.<Chain>/ARC76Account.<Chain>.csproj`.

4. **Create `ARC76Account.<Chain>/ARC76.cs`** with namespace `ARC76Account.<Chain>`, following
   `ARC76Account.Ethereum/ARC76.cs` as the template:
   ```c#
   using ARC76Account.Core;

   namespace ARC76Account.<Chain>
   {
       public static class ARC76
       {
           public static <ChainKeyType> GetAccount(string password, int slot = 0)
           {
               var seed = Arc76Seed.Derive(password, slot);
               return /* build chain key from seed */;
           }

           public static <ChainKeyType> GetEmailAccount(string email, string password, int slot = 0)
           {
               var seed = Arc76Seed.Derive(email, password, slot);
               return /* build chain key from seed */;
           }
       }
   }
   ```
   - Call `Arc76Seed.Derive(...)` — do not re-implement the PBKDF2 call inline.
   - Keep the `slot = 0` default parameter.
   - **Namespace collision check**: if the chain's SDK root namespace has the same simple name
     you're about to give the new namespace (as happens with `Algorand` — see `AGENTS.md`'s
     "Namespace gotcha" section), either pick a non-colliding namespace name or qualify SDK
     references with `global::` inside that file. Verify with a build, not just by reading the
     code.

5. **Add a `ProjectReference`** to the new project from `TestARC76/TestARC76.csproj`.

6. **Add known-answer tests** in a new `TestARC76/<Chain>Tests.cs` file, mirroring
   `EVMTests.cs`/`BitcoinTests.cs`: one test for the password variant, one for the email
   variant, asserting the derived public address and private key against fixed expected values
   for the test password `"12345678901234567890123456789012345678901234567890"` (the convention
   used everywhere else in this repo). Reference the new chain via its full namespace, e.g.
   `ARC76Account.<Chain>.ARC76.GetAccount(...)`. Run `dotnet test` and copy the actual output
   into the assertions rather than hand-computing expected values — these are regression tests,
   not spec tests. (If the chain SDK's address/address-type API isn't obvious, it's fine to
   verify it first in a disposable scratch console project before writing the real test.)

7. **Update `dotnet/README.md`**:
   - Add a row to the "Supported chains" table (chain, package, namespace, type returned).
   - Add an install line to the "Install" section's package list.
   - Add a usage snippet in the same style as the existing chain sections (code block +
     `Assert.That` example lines showing real derived values).

8. **Update `dotnet/AGENTS.md`** and **`dotnet/AI_INTEGRATION.md`** with the new chain (see how
   Bitcoin/Bitcoin Cash/Solana were documented there) so future agents/AI assistants know about
   it. Also add one line to the repo-root `AGENTS.md`'s chain list if it enumerates chains
   (check current content — it may just point at `dotnet/AGENTS.md` instead).

9. **Double check nothing else changed.** `ARC76Account.Algorand.ARC76.*`,
   `ARC76Account.Ethereum.ARC76.*`, etc. must be byte-for-byte unchanged, and no existing
   project's `.csproj` should have gained a new `PackageReference` — a diff touching either is a
   sign the new chain's code or dependency landed in the wrong project.

## Non-goals

- Don't add a generic chain-agnostic interface/factory across `ARC76Account.<Chain>.ARC76`
  classes unless explicitly asked — the flat, per-package pattern is deliberate (see
  `dotnet/README.md` → "Adding a new chain").
- Don't touch `ARC76Account.Core/Arc76Seed.cs`'s PBKDF2 parameters (iterations, hash, seed
  size) — those are shared across all chains and are part of the ARC76 spec; changing them
  breaks every chain's addresses at once.
- Don't add the new chain's SDK package to any project other than the new chain's own — that
  defeats the purpose of the per-chain package split.
