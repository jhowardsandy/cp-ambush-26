# Tactical Strategy Game Agent Instructions

## Vision

Build an original squad-level tactical strategy game where individual orders resolve over one shared timeline. It is inspired by design philosophy, not protected expression, of historical tactical games.

## Non-negotiable rules

1. Keep `Game.Core` deterministic, plain C#, and free of Unity scene, rendering, physics, input, frame-time, wall-clock, and network dependencies.
2. Presentation renders simulation events; it never determines outcomes.
3. Randomness is centralized, seeded, reproducible, and recorded in replay inputs.
4. Preserve the dependency direction: Presentation/Editor/Tests -> Core. Core must not reference Unity assemblies.
5. Do not silently invent major game rules. Record meaningful assumptions and accepted decisions in `docs/`.
6. Add or update behavior-focused tests for every core behavior change.
7. Keep content data-driven where practical.
8. Do not copy protected code, names, art, maps, rulebook text, audio, or branding.

## Source of truth

1. Direct task instructions from Justin.
2. This file.
3. Accepted ADRs in `docs/decisions/`.
4. Technical design documents.
5. Game design documents.
6. Existing implementation.
7. Agent assumptions.

## Definition of done

Document behavior, cover core logic with automated tests, preserve deterministic replay, handle invalid states, and avoid unrelated systems.
