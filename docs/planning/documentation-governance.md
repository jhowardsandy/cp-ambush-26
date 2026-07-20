# Documentation Governance

## Canonical documentation

The source repository contains implementation-adjacent documentation: rulebook chapters, technical contracts, calculation sheets, traceability, fixtures, ADRs, and roadmap detail. The Obsidian vault contains the durable product record, roadmap status, strategic decisions, and portfolio context.

## Change protocol

1. Give every material rule a stable domain prefix and sequential ID.
2. Write or update the rulebook before changing its core behavior.
3. Update the technical design, calculation examples, traceability row, and test fixtures in the same change.
4. Mark replay/schema/content-version compatibility explicitly.
5. Add a concise rule-change entry when a released or accepted behavior changes.
6. Update the Obsidian product record for material milestone, architecture, risk, or roadmap changes.

## Required diagrams

Maintain Mermaid source diagrams for the rule-delivery lifecycle, engine/settings boundaries, event ordering, and each complex conflict policy. Keep diagrams alongside the documents they explain so they review cleanly in Git and render in Markdown/Obsidian.
