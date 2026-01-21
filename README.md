# Agent-First Template

A template repository for agent-driven development with pre-configured agents to streamline the complete feature development lifecycle from ideation through implementation, testing, documentation, and review.

## Available Agents

This template provides six specialized agents that work together to deliver high-quality features:

### 1. Feature Brainstorming Agent 💡

**Purpose**: Explore and refine feature ideas through Socratic questioning

### 2. Feature Decomposition Agent 🗂️

**Purpose**: Break down features into actionable sub-issues

### 3. Architecture Agent 🏗️ *NEW*

**Purpose**: Design comprehensive technical architecture for complex features

### 4. API Design Agent 🔌

**Purpose**: Create detailed API specifications and contracts

### 5. Documentation Agent 📚 *NEW*

**Purpose**: Generate and maintain comprehensive technical documentation

### 6. Code Review Agent 🔍 *NEW*

**Purpose**: Automated first-pass code review with actionable feedback

---

## Quick Start

### For New Features

```
1. 💡 Brainstorm → Define the feature
2. 🗂️ Decompose → Break into tasks
3. 🏗️ Architecture (if complex) → Design system
4. 🔌 API Design (if APIs) → Specify contracts
5. ⚡ Implement → Build it
6. 📚 Documentation → Generate docs
7. 🔍 Code Review → Automated review
8. ✅ Merge → Ship with confidence
```

**For complete workflow guidance, see [WORKFLOW.md](.github/agents/WORKFLOW.md)**

---

## Detailed Agent Documentation

### Feature Brainstorming Agent 💡

Use the brainstorming agent to explore and refine new feature ideas through Socratic questioning.

**Location**: `.github/agents/brainstorm.md`

**How to use**:
1. Invoke the brainstorming agent through your GitHub Copilot interface
2. The agent will guide you through a structured brainstorming session
3. Answer questions one at a time - the agent will adapt to your responses
4. After 10-15 questions, the agent will create a GitHub issue ready for implementation

**What you'll get**:
- A well-structured GitHub issue with:
  - Clear problem statement
  - Proposed solution
  - User impact analysis
  - Success metrics
  - Technical considerations
  - MVP scope and future enhancements
  - Risk assessment

**Example conversation flow**:
```
Agent: What problem are you trying to solve with this new feature?
You: [Your answer]

Agent: Who experiences this problem most acutely?
You: [Your answer]

... (continues for 10-15 questions)

Agent: [Generates comprehensive GitHub issue]
```

### Feature Decomposition Agent

Use the decomposition agent to break down comprehensive GitHub issues into actionable sub-issues that can be assigned to developers or coding agents.

**Location**: `.github/agents/decompose.md`

**How to use**:
1. Invoke the decomposition agent with a GitHub issue (typically created by the brainstorm agent)
2. The agent will analyze the feature and confirm its understanding
3. It will explain its decomposition strategy
4. The agent will generate detailed sub-issues with acceptance criteria, dependencies, and implementation order

**What you'll get**:
- A complete decomposition with:
  - Analysis summary of the feature breakdown
  - Execution plan with recommended implementation order
  - Detailed sub-issues (typically 5-15) including:
    - Clear, action-oriented titles
    - Specific acceptance criteria (3-5 per issue)
    - Technical details and dependencies
    - Suggested labels and complexity estimates
    - Parent-child relationships
  - Critical path identification
  - Opportunities for parallel work

**Example workflow**:
```
You: [Provide GitHub issue URL or content]

Agent: [Analyzes and confirms understanding]

Agent: [Explains decomposition strategy]

Agent: [Generates 9 sub-issues organized into 5 phases]
- Phase 1: Infrastructure & Data
- Phase 2: Backend Services  
- Phase 3: Frontend Integration
- Phase 4: Integration & Polish
- Phase 5: Validation

[Each sub-issue includes full details ready for implementation]
```

**Example**: See `.github/agents/decompose-example.md` for a complete decomposition session

### Code Implementation Agent 🔨

Use the code implementation agent to create comprehensive implementation plans for well-defined sub-issues with production-ready code guidance.

**Location**: `.github/agents/code-implementation.md`

**How to use**:
1. Invoke the code implementation agent with a sub-issue (typically from the decompose agent)
2. The agent analyzes the codebase for patterns and conventions
3. It checks for architecture/API documentation
4. The agent generates a detailed implementation plan with code examples

**What you'll get**:
- A comprehensive implementation plan with:
  - Context from architecture docs and existing codebase patterns
  - Acceptance criteria mapped to implementation tasks
  - Step-by-step implementation guidance with code examples
  - Files to create/modify with full paths
  - Code patterns to follow (error handling, logging, auth)
  - Edge cases and security considerations
  - Performance and accessibility guidance
  - Pre/post implementation checklists

**Example workflow**:
```
You: [Provide sub-issue with acceptance criteria]

Agent: [Analyzes codebase patterns and confirms understanding]

Agent: [Generates implementation plan with:]
- Database schema changes
- Service class implementation with code examples
- API integration guidance
- Error handling patterns
- Security considerations
- Testing suggestions

[Ready to implement or pass to autonomous coding agent]
```

**Example**: See `.github/agents/code-implementation-example.md` for a complete implementation planning session

### Test Generation Agent 🧪

Use the test generation agent to create comprehensive test coverage for implemented features, mapping acceptance criteria to test cases.

**Location**: `.github/agents/test-generation.md`

**How to use**:
1. Invoke the test generation agent after implementing a feature
2. The agent analyzes the implementation code
3. It detects the test framework and existing patterns
4. The agent generates comprehensive unit, integration, and E2E tests

**What you'll get**:
- Comprehensive test coverage with:
  - Acceptance criteria mapped to test cases
  - Unit tests for individual functions/methods
  - Integration tests for API endpoints
  - E2E tests for user workflows (when applicable)
  - Test fixtures and mock data
  - Edge case and error scenario coverage
  - Security and performance test guidance
  - Expected coverage report (>80%)

**Example workflow**:
```
You: [Provide implementation reference]

Agent: [Analyzes code and test framework]

Agent: [Generates test suite with:]
- Unit tests with proper mocking
- Integration tests with database setup
- E2E tests for critical paths
- Test coverage analysis
- Running instructions

[Ready to run tests and verify coverage]
```

**Example**: See `.github/agents/test-generation-example.md` for a complete test generation session

## Agent-First Development Workflow

This template supports a complete agent-driven development workflow:

```
1. Brainstorm → 2. Decompose → 3. Implement → 4. Test → 5. Review
   (brainstorm)    (decompose)    (code-impl)    (test-gen)  (human/agent)
```

**Step 1: Brainstorm** - Use the brainstorming agent to explore and refine feature ideas into comprehensive GitHub issues

**Step 2: Decompose** - Use the decomposition agent to break down the issue into actionable sub-issues with acceptance criteria

**Step 3: Implement** - Use the code implementation agent to create detailed implementation plans, then implement following the guidance (manually or with autonomous coding agents)

**Step 4: Test** - Use the test generation agent to create comprehensive test coverage that validates all acceptance criteria

**Step 5: Review** - Human review and merge, or use review agents

This workflow ensures features are well-thought-out, properly scoped, systematically implemented, thoroughly tested, and ready for production.

## Adding More Agents

To add additional agents to this template:

1. Create a new markdown file in `.github/agents/`
2. Define the agent's purpose, behavior, and output format
3. Update this README with usage instructions
4. Optionally create an example file showing the agent in action

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## License

MIT License - feel free to use this template for your projects.

---

### Architecture Agent 🏗️

Design comprehensive technical architecture for complex features including system components, data models, and integration patterns.

**Location**: `.github/agents/architecture.md`

**When to use**:
- ✅ Complex features (complexity 7+ out of 10)
- ✅ New infrastructure components
- ✅ Database schema changes
- ✅ Multi-component integrations
- ✅ Performance or security-critical features

**How to use**:
1. Invoke the architecture agent with a sub-issue from decomposition
2. Provide context: performance requirements, existing architecture, constraints
3. The agent performs complexity assessment
4. For complex features, generates comprehensive architecture design
5. Agent detects if APIs are needed and recommends API Design Agent

**What you'll get**:
- Complete architecture design document with:
  - System architecture with component diagrams (Mermaid)
  - Data models and database schema with migrations
  - Architecture Decision Records (ADRs) for key choices
  - Technology stack decisions with rationale
  - Integration patterns for existing systems
  - Security, performance, and scalability considerations
  - Deployment strategy and infrastructure requirements
  - API detection with recommendations

**Example**: See `.github/agents/architecture-example.md` for a complete WebSocket architecture

---

### API Design Agent 🔌

Create detailed, production-ready API specifications for REST, GraphQL, WebSocket, and gRPC interfaces.

**Location**: `.github/agents/api-design.md`

**When to use**:
- ✅ Recommended by Architecture Agent (APIs detected)
- ✅ Creating new APIs (REST, GraphQL, WebSocket, gRPC)
- ✅ Modifying existing API contracts
- ✅ External-facing APIs

**Can work**:
- With architecture context (Recommended for complex APIs)
- Standalone (Good for simple CRUD additions)

**How to use**:
1. Invoke the API Design Agent with sub-issue or feature description
2. Agent checks for architecture context
3. Provide API requirements: type, consumers, existing patterns
4. Agent generates comprehensive API specifications
5. Review OpenAPI/AsyncAPI spec

**What you'll get**:
- Complete API specification with:
  - Endpoint definitions with full request/response details
  - Authentication and authorization patterns
  - Error handling with consistent responses
  - Rate limiting strategy
  - Security considerations (OWASP Top 10)
  - OpenAPI 3.0 / AsyncAPI 2.0 specification
  - Code examples for multiple platforms
  - Implementation guidance

**Example**: See `.github/agents/api-design-example.md` for a complete WebSocket protocol design

---

### Documentation Agent 📚

Generate and maintain comprehensive technical documentation throughout the development lifecycle.

**Location**: `.github/agents/documentation.md`

**When to use**:
- ✅ After implementing new features
- ✅ Creating or updating APIs
- ✅ Adding new components or libraries
- ✅ When documentation gaps are identified

**How to use**:
1. Invoke the documentation agent with code or PR to document
2. Specify documentation types needed (API, code comments, user guide, changelog)
3. The agent analyzes code and existing documentation
4. Agent generates comprehensive documentation with examples
5. Review and integrate into repository

**What you'll get**:
- Complete documentation suite with:
  - API reference with request/response examples
  - Code documentation (JSDoc/docstrings)
  - User guides and tutorials
  - Changelog entries
  - README updates
  - Multiple language examples (JavaScript, Python, etc.)
  - Gap identification and maintenance recommendations

**Example**: See `.github/agents/documentation-example.md` for complete documentation generation

---

### Code Review Agent 🔍

Automated first-pass code review with comprehensive, actionable feedback across multiple quality dimensions.

**Location**: `.github/agents/code-review.md`

**When to use**:
- ✅ Before requesting human code review
- ✅ After implementation and testing
- ✅ For automated quality gates in CI/CD
- ✅ When you want comprehensive feedback

**How to use**:
1. Invoke the code review agent with PR URL or code changes
2. Provide acceptance criteria and design docs (if available)
3. Agent performs comprehensive review across all categories
4. Agent provides prioritized, actionable feedback
5. Address critical and high-priority issues
6. Re-review if significant changes made

**What you'll get**:
- Comprehensive code review with:
  - Security vulnerability scanning (SQL injection, XSS, auth issues)
  - Code quality analysis (duplication, complexity, best practices)
  - Performance optimization suggestions (N+1 queries, caching)
  - Test coverage validation (>80% threshold)
  - Documentation completeness check
  - Accessibility validation (WCAG for UI)
  - Severity-based prioritization (critical/high/medium/low)
  - Specific line numbers and code examples
  - Estimated fix time for each issue

**Example**: See `.github/agents/code-review-example.md` for complete review session

---

## Complete Workflow

The agent-first approach now includes technical design and quality assurance phases for a complete development lifecycle:

```
1. Brainstorm → 2. Decompose → 3. Architecture* → 4. API Design* → 5. Implement → 6. Test → 7. Documentation → 8. Code Review → 9. Merge
   (brainstorm.md)  (decompose.md)  (architecture.md)  (api-design.md)  (coding agents) (test agents) (documentation.md) (code-review.md) (human review)
   
* Conditional - Based on feature complexity
```

### When to Use Each Agent

| Feature Type | Brainstorm | Decompose | Architecture | API Design | Documentation | Code Review |
|--------------|------------|-----------|--------------|------------|---------------|-------------|
| **New major feature** | ✅ | ✅ | ✅ | ⚠️ If APIs | ✅ | ✅ |
| **REST API (complex)** | ⚠️ | ⚠️ | ✅ | ✅ | ✅ | ✅ |
| **REST API (simple)** | ❌ | ❌ | ❌ | ✅ | ✅ | ✅ |
| **WebSocket/Real-time** | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| **Database schema** | ⚠️ | ⚠️ | ✅ | ❌ | ✅ | ✅ |
| **UI-only feature** | ⚠️ | ⚠️ | ❌ | ❌ | ⚠️ | ✅ |
| **Bug fix** | ❌ | ❌ | ❌ | ❌ | ⚠️ | ✅ |

**✅** Recommended | **⚠️** Optional | **❌** Skip

**For complete workflow guidance, decision trees, and examples, see [.github/agents/WORKFLOW.md](.github/agents/WORKFLOW.md)**

---

## Benefits

### With Complete Development Lifecycle

The addition of Architecture, API Design, Documentation, and Code Review agents provides:

- **✅ 30-50% reduction** in implementation rework
- **✅ 80%+ of common issues** caught before human review
- **✅ 30-50% faster code review** with automated first-pass
- **✅ Consistent documentation** quality and completeness
- **✅ Clear specifications** eliminate blocking questions during implementation
- **✅ Consistent patterns** through standardized architecture and API design
- **✅ ADRs capture** decision rationale for future reference
- **✅ Higher quality** with security and performance considered upfront
- **✅ Faster onboarding** with comprehensive documentation
- **✅ Complete audit trail** from ideation to deployment

### Development Time Investment

- Brainstorm: 20-30 minutes
- Decompose: 15-30 minutes
- Architecture (if needed): 30-60 minutes
- API Design (if needed): 30-45 minutes
- Documentation: 10-15 minutes
- Code Review: 5-10 minutes (automated)

**Total upfront design + QA**: 1-3 hours for complex features

**ROI**: Prevents 30-50% implementation rework + catches 80%+ of issues early (days to weeks saved)

---

## Documentation

### Agent Instructions
- `.github/agents/brainstorm.md`
- `.github/agents/decompose.md`
- `.github/agents/architecture.md`
- `.github/agents/api-design.md`
- `.github/agents/documentation.md` 🆕
- `.github/agents/code-review.md` 🆕

### Examples
- `.github/agents/brainstorm-example.md`
- `.github/agents/decompose-example.md`
- `.github/agents/architecture-example.md`
- `.github/agents/api-design-example.md`
- `.github/agents/documentation-example.md` 🆕
- `.github/agents/code-review-example.md` 🆕

### Templates
- `.github/templates/adr-template.md`
- `.github/templates/design-doc-template.md`
- `.github/templates/openapi-template.yaml`

### Guides
- `.github/agents/USAGE.md` - Detailed usage guide
- `.github/agents/WORKFLOW.md` - Complete workflow with decision trees

