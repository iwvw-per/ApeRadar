# ApeRadar Domain Context

## Product

ApeRadar is a Windows desktop companion for World of Warships. It reads battle metadata written by the game, retrieves public player statistics, and presents team composition without interacting with the game process.

## Domain Language

### Battle

A match described by `tempArenaInfo.json` or the metadata prefix of a `.wowsreplay` file. A Battle has a battle type, start time, participants, allies, and enemies.

### Participant

A player or bot listed in Battle metadata. A Participant has a relation, server, ship identifier, and name before statistics are retrieved.

### Player Statistics

The normalized account, ship, solo, division, experience, damage, clan, hidden-profile, and weighted-win-rate data shown for a Participant.

### Statistics Source

An external provider used to retrieve Player Statistics. Current sources are Vortex, WG Public, and WG Public through the Yuyuko proxy.

### Battle Intake

The complete operation that parses a Battle, resolves servers, selects Statistics Sources, retrieves Player Statistics, applies the Watch List, calculates the Battlefield, and produces Output Text.

### Battlefield

The calculated presentation of a Battle: allies, enemies, team aggregates, sort projections, and chart inputs.

### Watch List

Per-server user data that marks players as Positive, Negtive, Cheater, or None. The historical `Negtive` spelling is a compatibility contract.

### Ship Catalog

Versioned local metadata mapping ship identifiers to localized names, type, and tier.

### Output Text

User-configurable text generated from Battlefield statistics for copying or display. Template tags and exact formatting are compatibility contracts.

### Replay Observation

Detection of a new `tempArenaInfo.json` under the configured game replay directory. Observation and parsing are separate responsibilities.

### Software Update

Download, hash validation, extraction, and replacement of application or Ship Catalog files while preserving runtime user data.

## Invariants

- Battle files are opened with sharing compatible with the game writing or replacing them.
- Bots excluded by the existing identifier rule remain excluded.
- Server selection and Statistics Source fallback preserve current regional behavior.
- Hidden or unavailable statistics retain the existing negative sentinel semantics until typed result states replace them.
- Watch List, settings, window placement, screenshots, and logs remain under their established user-data locations.
- Software Update never overwrites Watch List, window placement, logs, screenshots, or download runtime data.
