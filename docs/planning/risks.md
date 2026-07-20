# Risks and Assumptions

- Unity Hub/Personal licensing must be activated before command-line test execution.
- Unity `6000.3.20f1` is selected as the local Unity 6.3 LTS editor version.
- GUIDs are treated as stable input IDs; ordering is based on their canonical `N` string form.
- Command bundles must only command same-faction units.
- Invalid static commands reject the whole request without a partial event log.
- Units already inactive or incapacitated cause scheduled actions to fail at start.
- Milestone 1 deliberately makes no movement collision, pathfinding, targeting, combat, visibility, or reaction rule.
