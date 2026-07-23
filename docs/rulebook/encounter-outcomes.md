# Encounter Outcomes

## OUT-TERM-001: Data-defined terminal objectives

Status: proposed generic expansion; existing terminal objective types remain accepted.

Every scenario declares one or more terminal objectives. A terminal objective has a stable ID, winning faction, type, and explicit parameters. The reusable engine evaluates them only after a valid round resolves; Unity, AI, and animation never declare an outcome.

Initial terminal types are: eliminate opposing units, hold an area for completed rounds, rescue/extract, named commander defeat, survive through a round limit, and score threshold. Existing eliminate, hold, and rescue/extract behavior remains accepted; commander, survive, and score require their own implementation/tests before use.

## OUT-PRI-001: Outcome precedence and simultaneous completion

Status: proposed.

Scenario content assigns each terminal objective an integer priority. After a valid round:

1. Evaluate all terminal objectives against the same authoritative final state and objective progress.
2. If only one faction has a completed objective, that faction wins.
3. If both factions complete objectives, the higher priority wins.
4. If priorities tie, the encounter is a draw unless scenario content supplies an explicit deterministic tie-break.
5. Invalid rounds do not advance round limits, score, objective progress, or outcomes.

The result records every completed objective considered, not only the eventual winner, so replay inspection can explain a simultaneous result.

## OUT-LIMIT-001: Round-limit resolution

Status: proposed.

A scenario may define a maximum number of completed rounds. At that boundary, evaluate its explicit limit policy: defender win, attacker win, score comparison, or draw. A round limit is not an implicit loss for either faction. If score comparison ties, use the terminal tie policy above.

## OUT-SCORE-001: Secondary objective scoring

Status: proposed.

Secondary objectives never end the encounter by themselves unless the scenario promotes them to a terminal score-threshold objective. They contribute deterministic score/rewards for campaign and post-encounter evaluation: preserved units, rescued people, captured supplies, completed turns, or controlled sites. Each score source must name its faction, calculation, cap, and event/progress evidence.

## Riverside Crossing showcase fixture

The implemented dedicated acceptance fixture is **Secure the Ford**:

- Blue terminal win: hold Central Crossing for three uncontested completed rounds.
- Red terminal win: defeat the named Blue squad leader, or win on the twelve-round defender-limit policy.
- Pressure: Red reinforcements at defined rounds unless Blue controls a named signal/spawn-control area.
- Opening: deployment/contact positions guarantee a legal grenade opportunity, direct-fire exchange, cover choice, and healing opportunity.
- Demonstration: the implemented deterministic command script exercises grenade delivery, disabled reinforcement, and hold-area victory in three rounds. See `../quality/secure-the-ford-acceptance.md`.

This fixture is separate from free auto-play. Free auto-play remains an AI behavior probe and may stop at a safety cap; the showcase fixture is an acceptance scenario with an expected outcome/checksum.
