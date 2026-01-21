# ADR-XXX: [Title - Use Present Tense Verb Phrase]

**Status**: [Proposed | Accepted | Deprecated | Superseded by ADR-YYY]

**Date**: YYYY-MM-DD

**Decision Makers**: [Names or roles of people involved in this decision]

**Tags**: [e.g., architecture, database, api, security, performance]

---

## Context

### Problem Statement
[What is the issue or situation we're addressing? Describe the technical challenge, business need, or constraint that requires a decision.]

### Background
[Provide relevant background information:
- Current state of the system
- Why this decision is needed now
- What prompted this discussion
- Any previous attempts or related decisions]

### Constraints
[List any constraints that limit our options:
- Technical constraints (existing tech stack, performance requirements)
- Business constraints (budget, timeline, resources)
- Organizational constraints (team skills, company standards)
- Regulatory/compliance requirements]

### Assumptions
[State any assumptions we're making:
- About future growth/scale
- About user behavior
- About external dependencies
- About team capabilities]

---

## Decision

[Clearly state the decision that was made. Use present tense: "We will...", "We have decided to...", "The team has chosen to..."]

### Summary
[1-2 sentence summary of the decision]

### Details
[Provide specific details about the decision:
- What technology/approach/pattern was chosen
- How it will be implemented
- What changes to existing systems are needed
- Timeline or phases if applicable]

---

## Alternatives Considered

### Option 1: [Alternative Name]

**Description**: [What is this alternative?]

**Pros**:
- [Advantage 1]
- [Advantage 2]
- [Advantage 3]

**Cons**:
- [Disadvantage 1]
- [Disadvantage 2]
- [Disadvantage 3]

**Reason for rejection**: [Why we didn't choose this]

---

### Option 2: [Alternative Name]

**Description**: [What is this alternative?]

**Pros**:
- [Advantage 1]
- [Advantage 2]

**Cons**:
- [Disadvantage 1]
- [Disadvantage 2]

**Reason for rejection**: [Why we didn't choose this]

---

### Option 3: [Alternative Name]

[Repeat format for each alternative considered]

---

## Consequences

### Positive Consequences
- **[Consequence 1]**: [Description and impact]
- **[Consequence 2]**: [Description and impact]
- **[Consequence 3]**: [Description and impact]

### Negative Consequences
- **[Consequence 1]**: [Description and impact]
  - **Mitigation**: [How we'll address this]
- **[Consequence 2]**: [Description and impact]
  - **Mitigation**: [How we'll address this]

### Neutral Consequences
- **[Consequence 1]**: [Description - neither clearly positive nor negative]

---

## Risks & Mitigations

| Risk | Probability | Impact | Mitigation Strategy |
|------|-------------|--------|---------------------|
| [Risk 1] | High/Med/Low | High/Med/Low | [How we'll prevent or handle this] |
| [Risk 2] | High/Med/Low | High/Med/Low | [How we'll prevent or handle this] |

---

## Implementation

### Action Items
- [ ] [Specific task 1]
- [ ] [Specific task 2]
- [ ] [Specific task 3]

### Success Criteria
[How will we know this decision was successful?
- Measurable metrics
- Observable outcomes
- Timeline expectations]

### Timeline
- **Decision Date**: YYYY-MM-DD
- **Implementation Start**: YYYY-MM-DD
- **Expected Completion**: YYYY-MM-DD
- **Review Date**: YYYY-MM-DD (when we'll assess if this was the right choice)

---

## Related Decisions

- **ADR-XXX**: [Title and brief description of related decision]
- **ADR-YYY**: [Title and brief description of related decision]

---

## References

- [Link to relevant documentation]
- [Link to research or articles that informed this decision]
- [Link to GitHub issues, PRs, or discussions]
- [Link to proof of concepts or prototypes]

---

## Notes

[Any additional notes, learnings, or context that don't fit elsewhere but may be valuable for future reference]

---

## Updates

### YYYY-MM-DD
[Record significant updates or changes to this ADR after initial publication]

---

## Template Usage Guide

### When to Create an ADR

Create an ADR for decisions that:
- Have long-term architectural implications
- Are difficult or expensive to reverse
- Affect multiple teams or components
- Involve significant tradeoffs
- Set precedents for future decisions
- Answer questions like "Why did we choose X over Y?"

### When NOT to Create an ADR

Don't create ADRs for:
- Obvious or trivial decisions
- Decisions that can be easily reversed
- Implementation details that don't affect architecture
- Personal coding style preferences

### Best Practices

1. **Write for the future**: Assume readers won't have your context
2. **Be specific**: Include concrete examples and details
3. **Document alternatives**: Show you considered multiple options
4. **Keep it factual**: Focus on technical merits, not politics
5. **Update status**: Mark as superseded or deprecated when appropriate
6. **Link to related ADRs**: Show how decisions connect
7. **Review periodically**: Reassess decisions after implementation

### Common Tags

- `architecture` - Overall system design decisions
- `database` - Data modeling, storage, migrations
- `api` - API design and contracts
- `security` - Authentication, authorization, encryption
- `performance` - Optimization and scaling decisions
- `infrastructure` - Deployment, hosting, DevOps
- `integration` - Third-party services and integrations
- `testing` - Test strategy and tooling
- `monitoring` - Observability and alerting
