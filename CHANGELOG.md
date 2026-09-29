# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and this project follows
[Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- Version 1 visual guide with UML use cases, domain models, components, sequences, state transitions,
  readiness decisions, editable sources, and rendered diagrams.
- Initial Clean Architecture solution skeleton.
- Pivot.Framework package-source configuration.
- Initial Identity, Characters, Communities and Raids domain model.
- Domain events and repository contracts for aggregate roots.
- Architecture decision record for Discord-only authentication and Warmane-first integration.
- Combined raids in the domain: a raid requires one or more distinct instance and difficulty targets (#49).

### Changed

- Pull requests now use protected automatic squash merging and delete their source branches after merge (#46).
- Documentation contributions now pass root-file, style, link, diagram, and rendered-site checks before merging (#44).
