# Contributing to Planora

First off, thank you for considering contributing to Planora. It's people like you that make Planora such a great tool.

## Where do I go from here?

If you've noticed a bug or have a feature request, please make sure it hasn't already been addressed or reported by checking the issue tracker. If it hasn't, feel free to open a new issue.

## Ground Rules

* Ensure cross-platform compatibility for every change that's accepted.
* Create issues for any major changes and enhancements that you wish to make. Discuss things transparently and get community feedback.
* Keep feature branches focused. Do not combine multiple unrelated changes into a single pull request.

## Development Workflow

We use a feature-branching model. The `main` branch contains production-ready code, while the `dev` branch contains the latest development changes.

1. **Fork the repository** on GitHub.
2. **Clone your fork** locally.
3. **Add the upstream remote**: `git remote add upstream https://github.com/Planora-ai-AYAIR/core.git`
4. **Create a branch** for your feature or bug fix from the `dev` branch: `git checkout -b feature/your-feature-name dev` or `git checkout -b fix/your-bug-fix dev`

## Commit Message Guidelines

We follow the Conventional Commits specification. This leads to more readable messages that are easy to follow when looking through the project history.

Format:
`<type>(<scope>): <subject>`

Allowed `<type>` values:
* `feat`: A new feature
* `fix`: A bug fix
* `docs`: Documentation only changes
* `style`: Changes that do not affect the meaning of the code (white-space, formatting, missing semi-colons, etc)
* `refactor`: A code change that neither fixes a bug nor adds a feature
* `perf`: A code change that improves performance
* `test`: Adding missing tests or correcting existing tests
* `build`: Changes that affect the build system or external dependencies
* `ci`: Changes to our CI configuration files and scripts
* `chore`: Other changes that don't modify src or test files

Example:
`feat(parcels): add support for handling GeoJSON multi-polygons`

## Pull Request Process

1. Ensure any install or build dependencies are removed before the end of the layer when doing a build.
2. Update the README.md with details of changes to the interface, this includes new environment variables, exposed ports, useful file locations and container parameters.
3. Ensure your code conforms to the existing style guidelines.
4. Run all tests locally before submitting your Pull Request.
5. Create the Pull Request against the `dev` branch.
6. A project maintainer will review your Pull Request and either merge it, request changes, or close it with an explanation.

## Setting Up Your Development Environment

Please refer to the `README.md` file in the root directory for comprehensive instructions on setting up your local environment, including Docker containers, databases, and dependencies.

## Reporting Bugs

Bugs are tracked as GitHub issues. When you are creating a bug report, please include as many details as possible:

* Use a clear and descriptive title for the issue to identify the problem.
* Describe the exact steps which reproduce the problem in as many details as possible.
* Provide specific examples to demonstrate the steps.
* Describe the behavior you observed after following the steps and point out what exactly is the problem with that behavior.
* Explain which behavior you expected to see instead and why.
* Include screenshots and animated GIFs which show you following the described steps and clearly demonstrate the problem.

Thank you for contributing!
