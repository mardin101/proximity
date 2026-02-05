# Architecture Decision Records (ADRs)

This directory contains Architecture Decision Records for the proximity project.

## What are ADRs?

Architecture Decision Records (ADRs) document significant architectural decisions made during the project's lifecycle. They capture the context, decision, alternatives considered, and consequences, providing valuable historical context for future developers.

## Template

Use the ADR template located at `.github/templates/adr-template.md` to create new ADRs.

## Naming Convention

ADRs should follow this naming pattern:
- `ADR-001-short-descriptive-title.md`
- `ADR-002-another-decision.md`
- etc.

Where:
- Numbers are sequential (001, 002, 003, ...)
- Titles use lowercase with hyphens
- Titles should be brief but descriptive (3-7 words)

## ADR Index

| Number | Title | Status | Date | Tags |
|--------|-------|--------|------|------|
| 001 | [Use Modular Architecture with Plugin-Style Components](ADR-001-modular-architecture.md) | Accepted | 2026-01-21 | architecture |
| 002 | [Use Dependency Injection for Loose Coupling](ADR-002-dependency-injection.md) | Accepted | 2026-01-21 | architecture, testing |
| 003 | [Use WPF with MVVM for User Interface](ADR-003-wpf-mvvm-pattern.md) | Accepted | 2026-01-21 | architecture, ui |
| 004 | [No Installation Required - Portable Executable](ADR-004-portable-executable.md) | Accepted | 2026-01-21 | infrastructure, deployment |

## Creating a New ADR

To create a new ADR:

1. Determine the next sequential number
2. Copy the template: `cp .github/templates/adr-template.md docs/architecture/adrs/ADR-XXX-title.md`
3. Fill in all sections with relevant information
4. Set status to "Proposed"
5. Submit for review via pull request
6. Update status to "Accepted" when approved
7. Add entry to the index table above

## When to Create an ADR

Create an ADR for decisions that:

✅ **DO create ADRs for:**
- Technology stack choices (frameworks, databases, languages)
- Communication protocols and patterns (REST vs GraphQL, sync vs async)
- Data architecture decisions (schema design, storage approach)
- Security and authentication strategies
- Deployment and infrastructure choices
- Integration approaches with external systems
- Significant refactoring decisions
- Performance optimization strategies that affect architecture

❌ **DON'T create ADRs for:**
- Obvious or trivial decisions
- Decisions that can be easily reversed
- Implementation details that don't affect architecture
- Personal coding style preferences
- Minor bug fixes or tweaks

## ADR Statuses

- **Proposed**: Decision is under consideration
- **Accepted**: Decision has been approved and is active
- **Deprecated**: Decision is no longer recommended but may still be in use
- **Superseded by ADR-XXX**: Decision has been replaced by a newer ADR

## Best Practices

1. **Write for the future**: Assume readers won't have your current context
2. **Be specific**: Include concrete examples and technical details
3. **Document alternatives**: Show that multiple options were considered
4. **Keep it factual**: Focus on technical merits, not politics
5. **Update status**: Mark as superseded or deprecated when appropriate
6. **Link relationships**: Reference related ADRs and issues
7. **Review periodically**: Reassess decisions after implementation

## Common Tags

Use these tags to categorize ADRs:
- `architecture` - Overall system design
- `database` - Data modeling and storage
- `api` - API design and contracts
- `security` - Authentication, authorization, encryption
- `performance` - Optimization and scaling
- `infrastructure` - Deployment and DevOps
- `integration` - Third-party services
- `testing` - Test strategy
- `monitoring` - Observability

## Example ADRs

For examples of well-written ADRs, see:
- `.github/agents/architecture-example.md` - Contains sample ADRs within an architecture design
- `.github/templates/adr-template.md` - Template with detailed guidance

## Questions?

For questions about ADRs:
- Review the ADR template: `.github/templates/adr-template.md`
- Check the Architecture Agent guide: `.github/agents/architecture.md`
- See the workflow documentation: `.github/agents/WORKFLOW.md`
