# Architecture Documentation

This directory contains architecture documentation for the proximity project, following the Architecture Agent guidelines defined in `.github/agents/architecture.md`.

## Directory Structure

```
docs/architecture/
├── README.md                                      # This file
├── adrs/                                          # Architecture Decision Records
│   ├── README.md                                  # ADR index and guidelines
│   ├── ADR-001-modular-architecture.md            # Decision to use modular design
│   ├── ADR-002-dependency-injection.md            # Decision to use DI
│   ├── ADR-003-wpf-mvvm-pattern.md                # Decision to use WPF with MVVM
│   └── ADR-004-portable-executable.md             # Decision for no installation
└── designs/                                       # Design documents for major features
    ├── README.md                                  # Design document index
    └── proximity-mvp-foundation-design.md         # MVP Foundation architecture
```

## Architecture Decision Records (ADRs)

Architecture Decision Records capture important architectural decisions made during the project's lifecycle. They provide context, rationale, and consequences of decisions for future reference.

**Location**: `docs/architecture/adrs/`

**Template**: `.github/templates/adr-template.md`

**Naming Convention**: `ADR-XXX-short-title.md` (e.g., `ADR-001-use-websockets-for-notifications.md`)

### When to Create an ADR

Create an ADR for decisions that:
- Have long-term architectural implications
- Are difficult or expensive to reverse
- Affect multiple teams or components
- Involve significant tradeoffs
- Set precedents for future decisions

### ADR Workflow

1. Copy the ADR template from `.github/templates/adr-template.md`
2. Fill in all sections with relevant information
3. Set status to "Proposed"
4. Submit for review
5. Update status to "Accepted" when approved
6. Reference the ADR in related code and documentation

## Design Documents

Design documents provide comprehensive technical designs for complex features, including system components, data models, technology decisions, and integration patterns.

**Location**: `docs/architecture/designs/`

**Template**: `.github/templates/design-doc-template.md`

**Naming Convention**: `feature-name-design.md` (e.g., `notification-system-design.md`)

### When to Create a Design Document

Use the Architecture Agent (`.github/agents/architecture.md`) to create design documents for:
- Complex features (complexity 7+ out of 10)
- New infrastructure components
- Database schema changes
- Multi-component integrations
- Performance or security-critical features

### Design Document Workflow

1. Use the Architecture Agent to analyze feature complexity
2. If full architecture is needed, generate comprehensive design document
3. Review with team and stakeholders
4. Update ADRs for significant architectural decisions
5. Create API specifications if needed (using API Design Agent)
6. Proceed to implementation with design as reference

## Using the Architecture Agent

The Architecture Agent (`.github/agents/architecture.md`) helps create these artifacts through a structured process:

1. **Complexity Assessment**: Determines if full architecture design is needed
2. **Requirements Clarification**: Gathers necessary context
3. **Architecture Design**: Generates comprehensive technical design
4. **API Surface Detection**: Identifies API needs
5. **Next Steps**: Provides clear handoff and recommendations

See `.github/agents/architecture-example.md` for a complete example session.

## Related Resources

- **Architecture Agent**: `.github/agents/architecture.md`
- **Architecture Example**: `.github/agents/architecture-example.md`
- **ADR Template**: `.github/templates/adr-template.md`
- **Design Doc Template**: `.github/templates/design-doc-template.md`
- **Workflow Guide**: `.github/agents/WORKFLOW.md`
- **Usage Guide**: `.github/agents/USAGE.md`

## Best Practices

### For ADRs
1. Write for the future - assume readers won't have your context
2. Be specific - include concrete examples and details
3. Document alternatives - show you considered multiple options
4. Keep it factual - focus on technical merits
5. Update status - mark as superseded or deprecated when appropriate

### For Design Documents
1. Use diagrams liberally (Mermaid format preferred)
2. Document key decisions with ADRs
3. Provide specific, actionable recommendations
4. Reference existing code patterns when possible
5. Adapt detail level to complexity
6. Keep documents up-to-date as implementation progresses

## Contributing

When adding architecture documentation:

1. Follow the templates provided
2. Use clear, descriptive titles
3. Link related documents (ADRs, design docs, issues)
4. Keep diagrams simple and focused
5. Review with team before finalizing
6. Update this README if adding new sections or conventions

## Questions?

For questions about architecture documentation:
- Review the Architecture Agent guidelines: `.github/agents/architecture.md`
- Check the example session: `.github/agents/architecture-example.md`
- See the complete workflow: `.github/agents/WORKFLOW.md`
