# Architecture Design Documents

This directory contains comprehensive technical design documents for complex features and systems in the proximity project.

## What are Design Documents?

Design documents provide detailed technical blueprints for implementing complex features. They include system architecture, data models, technology decisions, integration patterns, and more. These documents are typically generated using the Architecture Agent.

## Template

Use the design document template located at `.github/templates/design-doc-template.md` to create new design documents.

## Naming Convention

Design documents should follow this naming pattern:
- `feature-name-design.md`
- `system-name-design.md`

Examples:
- `notification-system-design.md`
- `real-time-collaboration-design.md`
- `authentication-service-design.md`

## Design Document Index

| Feature | Status | Date | Related ADRs | Related Issues |
|---------|--------|------|--------------|----------------|
| [Proximity MVP Foundation](proximity-mvp-foundation-design.md) | Approved | 2026-01-21 | ADR-001, ADR-002, ADR-003, ADR-004 | [#12](https://github.com/mardin101/proximity/issues/12) |

## Creating a New Design Document

### Using the Architecture Agent (Recommended)

1. Invoke the Architecture Agent (`.github/agents/architecture.md`)
2. Provide the feature/sub-issue to be designed
3. The agent will:
   - Assess complexity (determine if full design is needed)
   - Gather requirements and constraints
   - Generate comprehensive design document
   - Identify ADRs that should be created
   - Detect API needs and recommend API Design Agent
4. Save the generated document to this directory
5. Create associated ADRs for significant decisions
6. Update the index table above

### Manual Creation

1. Copy the template: `cp .github/templates/design-doc-template.md docs/architecture/designs/feature-name-design.md`
2. Fill in all sections systematically
3. Use Mermaid for diagrams
4. Create ADRs for significant architectural decisions
5. Submit for review via pull request
6. Update the index table above

## When to Create a Design Document

Use the Architecture Agent to create design documents for features with:

✅ **DO create design docs for:**
- **High complexity** (7-10 out of 10):
  - Multi-component features
  - New infrastructure
  - Complex integrations
  - Database schema changes
  - Performance-critical features
  - Security-critical features
- **Medium complexity with uncertainty** (4-6 out of 10):
  - When team needs clarity before starting
  - When multiple approaches are possible
  - When coordination across teams is needed

❌ **DON'T create design docs for:**
- **Low complexity** (1-3 out of 10):
  - Simple UI changes
  - Bug fixes
  - Straightforward CRUD operations
  - Minor refactoring
- **When you can start coding immediately**:
  - Clear requirements
  - Well-established patterns
  - No architectural implications

## Design Document Structure

A comprehensive design document includes:

### Core Sections
1. **Executive Summary** - High-level overview
2. **System Architecture** - Component diagrams and descriptions
3. **Data Architecture** - Data models and database schemas
4. **Technology Stack** - Technology choices with rationale

### Decision Documentation
5. **Architecture Decision Records** - ADRs for key decisions
6. **Integration Points** - How system connects with others
7. **Security Considerations** - Authentication, authorization, data protection
8. **Performance Considerations** - Load, latency, optimization

### Operational Aspects
9. **Scalability Plan** - How to handle growth
10. **Error Handling & Resilience** - Failure scenarios and mitigations
11. **Testing Strategy** - Unit, integration, performance tests
12. **Deployment Strategy** - How to roll out the feature

### Project Management
13. **Technical Risks** - Risks and mitigation strategies
14. **Next Steps** - Handoff and recommendations

## Best Practices

### For Writing Design Docs
1. **Use diagrams liberally** - Mermaid format for consistency
2. **Document alternatives** - Show options considered
3. **Be specific** - Concrete examples and details
4. **Reference existing patterns** - Link to relevant code
5. **Adapt to complexity** - More detail for complex features
6. **Keep it updated** - Revise as implementation progresses

### For Using Design Docs
1. **Start with the diagram** - Understand visual structure first
2. **Read ADRs** - Understand key decisions and rationale
3. **Check integration points** - Understand dependencies
4. **Review security/performance** - Don't skip these sections
5. **Follow the testing strategy** - Ensure adequate coverage

## Relationship with ADRs

Design documents and ADRs work together:

- **Design docs** = Comprehensive technical blueprint for a feature
- **ADRs** = Individual architectural decisions within that design

**Workflow:**
1. Create design document for complex feature
2. Extract significant architectural decisions
3. Create separate ADR for each major decision
4. Link ADRs in design document
5. Reference design doc in ADRs

This separation allows:
- Design doc to provide holistic view
- ADRs to track specific decisions that may apply to multiple features

## Example Design Documents

For examples of well-written design documents, see:
- `.github/agents/architecture-example.md` - Complete example of WebSocket notification system design

## Design Document Statuses

- **Draft**: Initial version, under development
- **In Review**: Submitted for team review
- **Approved**: Reviewed and approved, ready for implementation
- **Implemented**: Feature has been built per this design

## Questions?

For questions about design documents:
- Review the Architecture Agent: `.github/agents/architecture.md`
- Check the example session: `.github/agents/architecture-example.md`
- See the design template: `.github/templates/design-doc-template.md`
- Read the workflow guide: `.github/agents/WORKFLOW.md`
