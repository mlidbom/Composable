# Retiring a NuGet package identity

Renaming a packable project, folding one project into another, or deleting one, all leave the same thing
behind: a package id on nuget.org that we will never publish under again. It keeps showing up in search,
it keeps resolving as a real package, and nothing about it says "this moved". Retiring the identity is the
step that closes that gap.

## What retiring means

nuget.org does not support deleting a package — deleting would break every build that restores it. What it
supports is **unlisting**: the version stops appearing in search and stops taking part in version
resolution, but it still exists and still restores by exact version. So unlisting costs existing consumers
nothing and stops new adoption of an identity we have abandoned.

Two consequences worth knowing before you run anything:

- **Unlisting is one-way from the command line.** nuget.org offers no API to re-list; re-listing is a
  per-version checkbox on the website. Read the plan before executing it.
- **Unlisting says nothing about where the package went.** For that there is **deprecation** — see below.

## The record: `RetiredNuGetPackageIds.txt`

One file at the repository root, one line per retired id, naming what replaced it. Nothing gets unlisted
that is not recorded there. That is deliberate: an automatic "listed on nuget.org but not in the solution"
diff would also catch every Compze package published from its own repository, and unlisting one of those
would be very hard to undo.

## The two commands

Both live in DevScripts, so import the module first:

```powershell
Import-Module ./DevScripts/Compze.psm1 -DisableNameChecking
```

**`C-Get-RetiredNuGetPackageStatus`** — reads only. Compares the record against the solution and against
nuget.org, and reports the three ways they can disagree: an id recorded as retired that a project now
publishes (RECLAIMED), an id still listed on nuget.org with no project left in the solution (UNRECORDED),
and a recorded id that still has listed versions (AWAITING UNLISTING). Run it after any release wave that
renamed or removed a packable project.

**`C-Unlist-RetiredNuGetPackages`** — prints the plan and stops. `-Execute` carries it out. It refuses to
run at all while any recorded id belongs to a project in the solution.

nuget.org's search index takes a few minutes to catch up, so re-run the status command after that rather
than immediately.

## One-time prerequisite: an Unlist-scoped API key

nuget.org scopes API keys separately for **Push** and **Unlist**, so the key CI publishes with cannot
unlist. Create a second key — nuget.org → your account → API Keys → Create, scope **Unlist**, glob pattern
`Compze.*` — and put it in the `NUGET_UNLIST_API_KEY` environment variable, or pass it as `-ApiKey`.

Keeping the two keys separate is the point, not an inconvenience: a key that could do both would let a
broken publish run hide packages.

This is why retiring is a local, deliberate, human-initiated action rather than a CI step. The unlist key
never needs to reach GitHub.

## Deprecate before unlisting, when the successor matters

Deprecation and unlisting solve different halves of the problem:

| | Unlisting | Deprecation |
|---|---|---|
| Hides from search | yes | no |
| Tells existing consumers anything | no | yes — reason and successor package |
| Surfaces in tooling | no | `dotnet list package --deprecated`, Visual Studio |
| Scriptable | yes | **no — website only** |

So for a renamed package, deprecating it with its successor is the more useful signal, and unlisting is
what stops new adoption. Doing both is normal. Deprecation is nuget.org → **Manage packages** →
**Deprecation**, where "Select all versions", reason **Legacy**, and an alternate package handles a whole
id in one pass. The successor to name is the one recorded in `RetiredNuGetPackageIds.txt`.

There is also a **popularity transfer** — a manual application to `account@nuget.org` that moves a
deprecated id's search ranking to its successor. It requires all versions deprecated and pointing at the
successor, and a **stable** version of the successor, so it is out of reach while everything is `0.x-alpha`.
Worth remembering for the 1.0 wave.
