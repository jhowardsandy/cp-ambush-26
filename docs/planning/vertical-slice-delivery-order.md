# Vertical-Slice Delivery Order

Status: accepted delivery strategy, 2026-07-21.

The engine is not considered “finished” before testing. Each phase closes only the rules needed by the next concrete vertical-slice proof; scenario findings then drive the next rule work.

## 1. Scenario-ready engine gate

Finish and review only the mechanics required for the first original PvE encounter: multi-unit order submission, AP accounting, movement, direct attacks, posture, terrain semantics, one-shot overwatch, deterministic enemy command generation, objectives, replay/inspection, and invalid-order explanations. Defer campaign persistence, advanced weapons, full stealth calculations, elevation, artillery, area effects, and polish until the scenario demonstrates their need.

## 2. Repeatable graybox PvE scenario

Build one playable original map with multiple units, terrain, several rounds, a simple objective, player orders, deterministic enemy orders, replay/reset controls, and an event inspector. Exercise distinct player decisions across repeated runs and convert every discovered calculation defect into a focused automated regression test.

## 3. Two-unit-type content proof

Define two small data-driven unit archetypes with explicit inventory/loadout, weapon stats, AP costs, health, skills/capabilities, and allowed actions. The proof must establish that generic actions (move/posture) and gated actions (for example overwatch or treatment) use the same capability/content contract rather than special-case code.

## 4. Mixed-roster scenario validation

Update the scenario to field two of each unit type for both teams. Test multiple tactical plans, AI choices, loadout/skill legality, objective completion, replay determinism, and human-readable calculations.

## 5. Readability art pass

Add only the first visual kit needed to validate tactical readability: terrain tiles/props, role-distinct unit placeholders, posture/overwatch/health/AP indicators, action feedback, and modest animations. Art remains subordinate to game semantics.

## 6. Expansion after the loop is trusted

Add more unit types, maps, PvE campaigns and story, original art/visual systems, voices, cutscenes, character reactions, sounds, richer animations, equipment/progression, and advanced tactical mechanics in vertical slices. A “simple v1” means a focused, explainable core loop—not a small ambition.
