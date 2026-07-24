# Changelog

All notable changes to Compze.Tessaging.Sqlite will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/).

## 0.3.0-alpha

- **The peer registry stores in an auxiliary database of its own: `«domain-database-name».PeerRegistry`.** An advertisement recording must commit while a domain transaction is open — a handler's send can be waiting, inside its open transaction, for exactly the peer knowledge the recording provides — and sqlite's per-database write gate would queue a domain-database commit behind that very transaction for the sender's whole handler-availability patience. In its own database the recording commits behind its own write gate (`ISqlitePeerRegistryConnectionPool`). A production composition configures the connection string under the derived name, exactly as with the type-id interner's `«domain-database-name».TypeIdInterner`; the testing hosts draw it from the test database pool.
- The tessaging sql layers store in the endpoint's prefixed table-set (`EndpointTableSet`, resolved from the container): the inbox, outbox, outbox-dispatching, and peer-memory tables are per-endpoint prefixed (`«EndpointName»_InboxTessages`), so any number of endpoints store side by side in one domain database, and `SchemaCreationSql` became a function of the table-set. New: `SqliteEndpointCatalogSqlLayer` — the domain database's one shared, deliberately unprefixed `EndpointCatalog` table recording each endpoint and the process holding its lock (the insert-if-absent is race-safe as written: sqlite serializes writers).
- The tessaging SQL layer implementations are async end to end (`SaveTessageAsync`, `MarkAsReceivedAsync`, `GetUndeliveredTessagesForEndpointAsync`, the inbox marks, the peer-registry members — matching the now-async `ITessagingSqlLayer`): exactly-once tessaging is database I/O, and synchrony-follows-the-type reaches the storage first. Same SQL, async command and reader plumbing.
- Adding exactly-once Tessaging on a sqlite foundation also wires the type-id interner, derived from the foundation's declaration (`SqliteTypeIdInterner(SqliteDomainDatabase)`) — on sqlite the interner has its own database, so the registrar-level `SqliteTessagingSqlLayer()` cannot demand it namelessly; registrar-level compositions register it explicitly.
- `SqliteDomainDatabase(connectionStringName)` on `ExactlyOnceEndpointBuilder`: declares that the domain database this endpoint joins is sqlite, filling the exactly-once endpoint's one domain-database parameter with the whole engine pairing — the connection pool, the sqlite type-id interner Tessaging's sql layers share (derived from the declaration), and Tessaging's sqlite inbox/outbox sql layers.
- **The storage follows the reliability work.** The endpoint catalog's sql layer carries a process **lock** rather than a lease — the heartbeat claim/renew/release statements are gone, replaced by a session-scoped lock held for the holder's lifetime. Two new per-endpoint tables serve exactly-once in-order delivery: the outbox's per-receiver delivery-stream counters and the inbox's delivery-stream admissions. The handling execution takes a row-level claim on its inbox row inside the handling transaction, and the inbox recovery scan re-enqueues admitted-but-unhandled tessages at start.
- The package shows only its registrar: every plumbing type is internal, reachable by the Compze packages granted `InternalsVisibleTo` and by nothing else.
- The visibility sweep reaches this package: non-public machinery moves below `_internal`/`_private` namespace sections — the markers that replaced the old `Internal`/`Private` spelling — and types and members are narrowed to the least visibility that compiles.

## 0.2.0-alpha

- Adding exactly-once Tessaging on a sqlite foundation also wires the type-id interner, derived from the foundation's declaration (`SqliteTypeIdInterner(SqliteEndpointDatabase)`) — on sqlite the interner has its own database, so the registrar-level `SqliteTessagingSqlLayer()` cannot demand it namelessly; registrar-level compositions register it explicitly.
- `AddExactlyOnceTessaging(compose)` on `EndpointFoundation<SqliteEndpointDatabase>`: adds exactly-once Tessaging to an endpoint whose database is sqlite, registering Tessaging's sqlite inbox/outbox sql layers — the engine pairing is routed by the compiler through the foundation's type.

## 0.1.0-alpha

- Initial pre-release
