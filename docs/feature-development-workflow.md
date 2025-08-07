# Feature Development Workflow

This document defines the standardized workflow for adding features using Claude Code's specialized subagents.

## Overview

The workflow ensures quality through multiple review stages while maintaining efficiency for different feature complexities.

## Workflow Stages

### 1. Initial Request & Requirements
**Trigger**: User requests a new feature

**Decision Point**: Is the request clear and detailed?
- **No** → Use `requirements-gatherer` agent to:
  - Define scope and requirements
  - Determine technical approach
  - Create initial specifications
- **Yes** → Proceed directly to Specification Phase

**Example**: 
- Unclear: "Add user notifications"
- Clear: "Add email notifications for password changes with admin toggle"

### 2. Specification Phase
**Primary Agent**: `doc-writer`
- Create detailed implementation specification
- Define technical architecture
- Document API contracts and data models

**Review Gate**: `reviewer` agent
- Review specification for:
  - Technical soundness
  - Completeness
  - Best practices alignment
  - Security considerations

**Decision Point**: Specification approved?
- **No** → Return to `doc-writer` for improvements
- **Yes** → Proceed to Implementation Phase

### 3. Implementation Phase
**Setup** (for major features): `git-manager`
- Create feature branch
- Set up proper branch naming conventions

**Primary Agent**: `dotnet-implementation-specialist`
- Implement the feature following the specification
- Follow existing code patterns and conventions
- Ensure proper error handling and logging

**Review Gate**: `reviewer` agent
- Code quality review
- Best practices adherence
- Performance considerations
- Security validation

**Decision Point**: Implementation approved?
- **No** → Return to `dotnet-implementation-specialist` for fixes
- **Yes** → Proceed to Finalization Phase

### 4. Finalization Phase
**Documentation Update**: `doc-writer`
- Update documentation based on final implementation
- Ensure accuracy of architectural documentation
- Update user-facing documentation if needed

**Testing** (if required): `test-writer`
- Create unit tests
- Create integration tests
- Validate test coverage

**Version Control**: `git-manager`
- Create commit with proper message
- Create pull request (if on feature branch)
- Tag releases if applicable

## Optional Specialized Stages

### Security Review
**When**: For authentication, authorization, or data handling features
**Agent**: `security-auditor`
- Review for vulnerabilities
- Validate input sanitization
- Check authentication/authorization logic

### Database Changes
**When**: Features requiring schema changes
**Agent**: `data-modeler`
- Design entity relationships
- Create migrations
- Optimize database performance

### UI Prototyping
**When**: Complex user interface features
**Agent**: `prototyper`
- Create HTML/CSS/JavaScript prototypes
- Validate design before backend implementation
- Ensure UI/UX consistency

## Workflow Examples

### Example 1: Simple Feature
**Request**: "Add a user preference for email frequency"

1. **Requirements**: Clear → Skip requirements-gatherer
2. **Specification**: doc-writer creates spec → reviewer approves
3. **Implementation**: dotnet-implementation-specialist implements → reviewer approves
4. **Finalization**: doc-writer updates docs → git-manager commits

### Example 2: Complex Feature
**Request**: "Add comprehensive audit logging system"

1. **Requirements**: requirements-gatherer defines scope and technical approach
2. **Specification**: doc-writer creates detailed spec → reviewer requests improvements → doc-writer revises → reviewer approves
3. **Setup**: git-manager creates feature branch
4. **Security**: security-auditor reviews audit requirements
5. **Database**: data-modeler designs audit tables and relationships
6. **Implementation**: dotnet-implementation-specialist implements → reviewer requests fixes → dotnet-implementation-specialist fixes → reviewer approves
7. **Testing**: test-writer creates comprehensive tests
8. **Finalization**: doc-writer updates documentation → git-manager creates commit and PR

### Example 3: UI-Heavy Feature
**Request**: "Add dashboard with charts and metrics"

1. **Requirements**: requirements-gatherer defines user needs and technical requirements
2. **Prototyping**: prototyper creates UI mockups and validates design
3. **Specification**: doc-writer creates implementation spec → reviewer approves
4. **Database**: data-modeler designs metrics storage
5. **Implementation**: dotnet-implementation-specialist implements → reviewer approves
6. **Finalization**: doc-writer updates docs → git-manager commits

## Best Practices

### Communication Between Agents
- Each agent receives complete context from previous stages
- Specifications are detailed enough for implementation without ambiguity
- Review feedback is specific and actionable

### Quality Gates
- No stage proceeds without approval from review gates
- Multiple revision cycles are acceptable for complex features
- Security and performance are validated at appropriate stages

### Branch Management
- Feature branches for significant changes
- Direct commits to main branch for minor features
- Proper commit messages following project conventions

### Documentation
- Living documentation updated with each feature
- Implementation details captured for future maintenance
- User-facing documentation updated when needed

## Agent Responsibilities Summary

| Agent | Primary Responsibility | When to Use |
|-------|----------------------|-------------|
| requirements-gatherer | Define scope and technical approach | Unclear or complex requirements |
| doc-writer | Create and maintain specifications and documentation | All features |
| reviewer | Quality assurance and best practices | All implementations and specs |
| dotnet-implementation-specialist | Code implementation | All features |
| git-manager | Version control and branch management | All features, especially major ones |
| security-auditor | Security validation | Security-sensitive features |
| data-modeler | Database design and optimization | Features with database changes |
| prototyper | UI mockups and validation | UI-heavy features |
| test-writer | Test creation and validation | Complex features or when requested |

## Workflow Flexibility

This workflow is designed to be:
- **Scalable**: Works for simple tweaks to complex features
- **Quality-focused**: Multiple review points ensure high standards
- **Efficient**: Skip unnecessary stages for simpler features
- **Consistent**: Standardized approach across all development