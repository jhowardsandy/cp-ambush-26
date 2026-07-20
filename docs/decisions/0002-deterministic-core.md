# ADR 0002: Deterministic Core

Status: Accepted.

Tactical outcomes are resolved by plain C# using a controlled seeded random provider. The core runs without a Unity scene, enabling tests, replays, debugging, balance harnesses, and future asynchronous validation.
