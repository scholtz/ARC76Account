# GitHub Actions secrets setup

This repo publishes NuGet packages via [`.github/workflows/publish-nuget.yml`](../.github/workflows/publish-nuget.yml),
npm packages via [`.github/workflows/publish-npm.yml`](../.github/workflows/publish-npm.yml),
and PyPI packages via [`.github/workflows/publish-pypi.yml`](../.github/workflows/publish-pypi.yml).
Each uses a different authentication method, and each needs a one-time setup outside this repo
before it will actually publish anything. This doc is the complete checklist.

## Summary

| Secret        | Used by              | What it is                              | Where it's created |
|---------------|-----------------------|------------------------------------------|----------------------|
| `NUGET_USER`  | `publish-nuget.yml`  | Your nuget.org **username** (not email)  | GitHub repo/environment secret |
| `NPM_TOKEN`   | `publish-npm.yml`    | An npm **Automation** access token       | GitHub repo/environment secret |
| *(none)*      | `publish-pypi.yml`   | PyPI Trusted Publishing needs no secret — just a policy configured on pypi.org (see [§3](#3-pypi-trusted-publishing-oidc-no-api-key-stored)) | — |

`publish-nuget.yml` and `publish-pypi.yml` need no *stored* secret at all — both use Trusted
Publishing/OIDC, which only needs a policy configured on the package registry's own site (plus,
for NuGet, a username to identify which policy to match). `publish-npm.yml` is the one exception,
using a classic token (see [§2](#2-npm-classic-token-based-publishing) for why).

## 1. NuGet: Trusted Publishing (OIDC, no API key stored)

NuGet.org's [Trusted Publishing](https://learn.microsoft.com/nuget/nuget-org/trusted-publishing)
lets the workflow exchange a short-lived GitHub-issued token for a temporary NuGet API key at
publish time — nothing long-lived is ever stored.

1. Sign in at [nuget.org](https://www.nuget.org) and go to **your username → Trusted
   Publishing**.
2. Add a new policy with:
   - **Repository Owner**: `scholtz`
   - **Repository**: `ARC76Account`
   - **Workflow File**: `publish-nuget.yml` (just the filename, not the `.github/workflows/`
     path)
   - **Environment** (optional): `release` — only fill this in if you keep the
     `environment: release` line in `publish-nuget.yml`; leave blank if you removed it.
3. You can either add one policy per package (`ARC76Account.Core`, `.Algorand`, `.Ethereum`,
   `.Bitcoin`, `.BitcoinCash`, `.Solana`) or, if the policy owner is your account/org, one
   policy that covers everything you own — nuget.org's UI will indicate which packages a
   policy applies to.
4. New policies on a **private** repo start in a 7-day "pending activation" window — the very
   first successful publish through this workflow makes it permanent. This repo is public, so
   this mostly doesn't apply, but if you ever see a "pending" status, just merge a real
   `dotnet/` change and let the workflow run once.
5. Add the `NUGET_USER` secret (see [§3](#3-adding-the-secrets-in-github) below) — this is your
   nuget.org **profile/username**, e.g. `scholtz`, not your email address.

That's it — no NuGet API key is generated or stored anywhere for this to work.

## 2. npm: classic token-based publishing

npm also has an OIDC "Trusted Publishing" feature, but it requires the package to already
exist on the registry before you can configure a trusted publisher for it, and (for scoped
packages) org-level setup that can be fiddly to get right the first time. Since `publish-npm.yml`
needs to work for six **brand-new** `@arc76/*` packages that have never been published before,
it uses a classic **npm access token** instead — simpler to set up today, and you can switch to
Trusted Publishing later once every package exists (see [§5](#5-optional-later-switching-npm-to-trusted-publishing)).

### 2a. Create (or confirm) the `@arc76` npm organization

All six packages are scoped as `@arc76/core`, `@arc76/algorand`, `@arc76/ethereum`,
`@arc76/bitcoin`, `@arc76/bitcoincash`, `@arc76/solana`. Scoped packages publish under an npm
**user** or **organization** matching the scope name.

1. Sign in at [npmjs.com](https://www.npmjs.com).
2. Go to **Add Organization** (from your avatar menu, or
   [npmjs.com/org/create](https://www.npmjs.com/org/create)).
3. Name it exactly `arc76` (so the scope becomes `@arc76`). The free plan is fine — these are
   all public packages, and public packages under a free org don't cost anything.
4. Make sure the account that will generate the token in the next step is a **member with
   publish rights** (Owner, or Admin/Developer with package publish permission) of this org.

### 2b. Generate an npm Automation token

CI/CD publishing needs an **Automation** token specifically — it's the token type designed to
bypass interactive 2FA/OTP prompts (a "Publish"-type classic token or a granular token without
the automation flag will otherwise stall waiting for a one-time password that a CI runner can
never provide).

1. On npmjs.com: **avatar menu → Access Tokens → Generate New Token → Granular Access Token**
   (or "Classic Token" → "Automation" if you prefer the classic token UI).
2. If using a **granular** token: scope it to the `@arc76` organization/packages, with
   **read and write** permission, and select **"Bypass two-factor authentication"** /
   "Automation" — this is the setting that makes it usable from CI.
3. Set an expiration you're comfortable with (granular tokens support this; rotate the
   `NPM_TOKEN` GitHub secret when it expires).
4. Copy the token value immediately — npm only shows it once.
5. Add it as the `NPM_TOKEN` secret (see [§4](#4-adding-the-secrets-in-github) below).

### 2c. Why `--access public` is also in the workflow

Scoped packages (`@arc76/...`) default to **private** on npm, which requires a paid plan.
Every package here sets `"publishConfig": { "access": "public" }` in its own `package.json`
*and* the workflow passes `--access public` on the `npm publish` command — belt and suspenders,
so a package never accidentally tries to publish as private and fail (or silently require a
paid plan) if one of the two is ever removed.

## 3. PyPI: Trusted Publishing (OIDC, no API key stored)

PyPI's [Trusted Publishing](https://docs.pypi.org/trusted-publishers/) works like NuGet's — the
workflow exchanges a short-lived GitHub-issued OIDC token for a temporary PyPI upload token at
publish time. Unlike npm at the time `publish-npm.yml` was written, PyPI supports registering a
**pending** trusted publisher for a project that doesn't exist yet, so all six `arc76-*`
packages can use OIDC from their very first publish — no interim token-based step needed.

1. Sign in at [pypi.org](https://pypi.org).
2. For each of the six packages (`arc76-core`, `arc76-algorand`, `arc76-ethereum`,
   `arc76-bitcoin`, `arc76-bitcoincash`, `arc76-solana`): go to
   [pypi.org/manage/account/publishing/](https://pypi.org/manage/account/publishing/) → **Add a
   pending publisher**, with:
   - **PyPI Project Name**: the package name, e.g. `arc76-core`
   - **Owner**: `scholtz`
   - **Repository name**: `ARC76Account`
   - **Workflow name**: `publish-pypi.yml` (just the filename, not the `.github/workflows/`
     path)
   - **Environment name** (optional): `release` — only fill this in if you keep the
     `environment: release` line in `publish-pypi.yml`; leave blank if you removed it.
3. The first successful publish for each project converts its pending publisher into a regular
   one automatically — no separate activation step, unlike NuGet's 7-day pending window.

That's it — no PyPI API token is generated or stored anywhere for this to work.

## 4. Adding the secrets in GitHub

Both `NUGET_USER` and `NPM_TOKEN` are added the same way — `publish-pypi.yml` needs no secret
at all (see [§3](#3-pypi-trusted-publishing-oidc-no-api-key-stored)). You can add the two
secrets as **repository** secrets (simpler) or **environment** secrets scoped to the `release`
environment (more secure — recommended if you keep the `environment: release` line in all three
workflows, since environment secrets are only readable by workflow runs targeting that
environment).

**Repository secrets:**
1. Go to the repo on GitHub → **Settings → Secrets and variables → Actions → Secrets** tab.
2. **New repository secret** → Name: `NUGET_USER` → Value: your nuget.org username → **Add secret**.
3. Repeat for `NPM_TOKEN` with your npm Automation token.

**Environment secrets** (if using the `release` environment gate):
1. **Settings → Environments** → create an environment named `release` if it doesn't exist yet
   (optionally add required reviewers here for a manual approval gate before every publish).
2. Inside that environment's page, under **Environment secrets**, add `NUGET_USER` and
   `NPM_TOKEN` the same way as above.
3. If you don't want the approval gate or the extra environment-scoping, delete the
   `environment: release` line from `publish-nuget.yml`, `publish-npm.yml`, and
   `publish-pypi.yml`, and just use repository secrets instead.

Either way, once both secrets exist and the nuget.org/pypi.org Trusted Publishing setups are
done, the next push to `master` touching `dotnet/**`, `typescript/**`, or `python/**` will
publish automatically — see the root [`README.md`](../README.md)'s "Automated publishing"
section for exactly which packages get published when.

## 5. (Optional, later) Switching npm to Trusted Publishing

Once every `@arc76/*` package has been published at least once via the token method above, you
can switch to OIDC Trusted Publishing the same way NuGet does it, and stop needing `NPM_TOKEN`
entirely:

1. For each `@arc76/*` package on npmjs.com: **package page → Settings → Trusted Publisher →
   Add GitHub Actions publisher**, with Organization/user `scholtz`, Repository `ARC76Account`,
   Workflow filename `publish-npm.yml` (and Environment `release` if you kept that line).
2. In `publish-npm.yml`: add `permissions: id-token: write` back to the job, remove the
   `NODE_AUTH_TOKEN: ${{ secrets.NPM_TOKEN }}` env from the Publish step, and use plain
   `npm publish /tmp/arc76-pack/*.tgz --access public` (npm ≥ 11.5.1 auto-detects OIDC from the
   `id-token: write` permission — no extra action needed, unlike NuGet's `NuGet/login@v1`).
3. You can then remove the `NPM_TOKEN` GitHub secret and revoke the npm Automation token.

This isn't required — the token-based setup above is fully supported and secure as long as the
token is scoped narrowly (Automation, `@arc76` only) and rotated periodically; switch to OIDC
whenever it's convenient, not urgently.
