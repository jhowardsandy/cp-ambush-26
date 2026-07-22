# Risks and Assumptions

- Unity Hub/Personal licensing must be activated before command-line test execution.
- Unity `6000.3.20f1` is selected as the local Unity 6.3 LTS editor version.
- GUIDs are treated as stable input IDs; ordering is based on their canonical `N` string form.
- Command bundles must only command same-faction units.
- Invalid static commands reject the whole request without a partial event log.
- Units already inactive or incapacitated cause scheduled actions to fail at start.
- Implemented rules intentionally remain narrow: direct fire has seeded base accuracy, target-tile cover, armor, ammunition, and one-shot reaction fire; area effects, projectile travel, accuracy modifiers, and expanded reaction policy remain explicit unresolved risks rather than implicit behavior.
- Present visibility is objective range/terrain sight only; faction memory, fog of war, elevation, concealment, and dynamic blockers remain unresolved.
- Larger scenario content is validated, but player-authored orders, PvE planning, deployment, richer objectives, and large-map performance have not yet been exercised in an interactive scenario.
- Unit/faction catalogs currently validate content only; capability IDs and numeric attributes are not silently applied to calculations until accepted rule integrations exist.
