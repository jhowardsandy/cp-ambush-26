# ADR 0006: Co-located Engine Until Extraction Criteria Are Met

Status: accepted, 2026-07-20.

## Decision

Keep the deterministic `Game.Core` engine and the first military game in this repository for the current rapid-prototyping phase. Preserve the existing dependency direction—presentation/editor/tests depend on core; core depends on no Unity APIs—so the engine can later become a versioned shared package or repository.

## Rationale

The engine contract is still actively gaining scenario, content, objective, combat, perception, replay, and planning behavior. Premature repository/package separation would add release/version coordination cost while interfaces are deliberately moving. The clean plain-C# assembly and test boundary provide the architectural separation needed now.

## Extraction criteria

Consider extracting a versioned engine package when all of the following are true:

1. A stable scenario/content schema and compatibility policy exist.
2. Replay/checksum fixtures cover the shared contract well enough to detect breaking changes.
3. At least one second game or independent prototype has a concrete need to consume the engine.
4. Game-specific presentation/content dependencies can be removed from the shared package without exceptions.
5. Semantic versioning, migration, and consumer upgrade policy are documented.

The military and future original fantasy games should then own their own content, presentation, assets, scenarios, marketing, and release cadence while depending on a shared deterministic engine version.
