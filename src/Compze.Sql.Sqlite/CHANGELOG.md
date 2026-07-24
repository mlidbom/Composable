# Changelog

All notable changes to Compze.Sql.Sqlite will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/).

## 0.4.0-alpha

- **The package sheds the `Internals` label: `Compze.Internals.Sql.Sqlite` is `Compze.Sql.Sqlite`.** The label told consumers not to depend on the package, which was never true of something they compose with deliberately. Previously published as [Compze.Internals.Sql.Sqlite](https://www.nuget.org/packages/Compze.Internals.Sql.Sqlite/) 0.3.0-alpha.
- `SqliteDomainDatabase`: the declaration that the domain database an endpoint joins is sqlite. `SqliteDomainDatabase(connectionStringName)` registers the connection pool, and the declaration type carries the connection-string name the sqlite pairings derive their wiring from — e.g. the type-id interner's own database name, which on sqlite lives in a separate database file.
- `SqliteMemoryDomainDatabase(databaseName)`: the public door to an in-memory sqlite domain database — the composition tests and samples reach for it directly instead of through test-only wiring.
- Auxiliary sqlite database pools: a consumer that needs a database of its own beside the domain database — its own file, and so its own per-database write gate — declares a pool type of its own and registers it through the auxiliary-database registrar, which mirrors the domain pool's configuration-vs-test-pool split. First consumer: the Tessaging peer registry on sqlite. The type-id interner's bespoke wiring predates the mechanism and remains.
- The endpoint process lock is held here: a session-scoped lock (an OS-level machine-wide mutex, sqlite having no server sessions) held for as long as the process that claimed the endpoint lives. It replaces the heartbeat lease, which could never tell a paused-but-alive holder from a dead one.
- The package shows only its doors: the domain-database declaration and its registrar are the public face; every plumbing type is internal behind `InternalsVisibleTo`.
- The visibility sweep reaches this package: non-public machinery moves below `_internal`/`_private` namespace sections — the markers that replaced the old `Internal`/`Private` spelling — and types and members are narrowed to the least visibility that compiles.

## 0.3.0-alpha

- `SqliteEndpointDatabase`: the declaration that an endpoint's database is sqlite, carried by `EndpointFoundation<SqliteEndpointDatabase>` so the features added on the foundation bind their sqlite sql layers through the compiler. The declaration itself lives here too — `SqliteEndpointDatabase(connectionStringName)` and its `ComposeEndpoint` composition form register the endpoint's connection pool; the sql-layer features wire their shared infrastructure (the type-id interner) themselves.

## 0.2.1-alpha

- README update.

## 0.2.0-alpha.1

Refactoring.

## 0.1.0-alpha.3

- Initial pre-release
