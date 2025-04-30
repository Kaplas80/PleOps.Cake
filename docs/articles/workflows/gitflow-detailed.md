# Git flow in detail

The [parent page](./gitflow.md) offers an overview of the proposed _Git flow_.
This document describes in detail each use case during development and
releasing.

## Developing

### New features and fixes

Developers implement new features and bug fixes from a new branch each time. The
feature branches have the prefix `feature/` and start from the `main` (or
`develop`) branch.

The recommended merging strategy is _squashing_ to maintain a clean history in
the main branch.

```mermaid
---
config:
  theme: base
  gitGraph:
    showCommitLabel: false
    mainBranchOrder: 1
---

gitGraph LR:
  commit tag: "Preview"

  branch feature/name order: 0
  commit tag: "Dev"
  commit tag: "Dev"
  checkout main
  merge feature/name tag: "Preview"
```

### Epic features

Large features may take several work batches to implement, breaking or making
the product unstable for a period of time. For these cases, consider having a
parallel branch to _main_ to merge related features. It allows breaking a large
work into smaller features, while not impacting other developers or the main
product development.

It also allows configuring different temporal branch policies, for instance
disable quality gates or even building some components, while the new work is
being implemented.

Depending on the work, consider configuring the _epic_ branch to build and
deploy on a separate environment for validation.

> [!TIP]  
> Prioritize merging the _epic_ branch as soon as possible. The maintenance cost
> increases quickly, and it can be tricky to merge back later. Consider doing
> periodic merges from main to the epic branch.

```mermaid
---
config:
  theme: base
  gitGraph:
    showCommitLabel: false
    mainBranchOrder: 2
---

gitGraph LR:
  commit tag: "Preview"

  %% Epic branch start
  branch epic/component-rewrite order: 3

  %% Regular feature
  branch feature/1 order: 0
  commit tag: "Dev"
  checkout main
  merge feature/1 tag: "Preview"

  %% Feature for the epic
  checkout epic/component-rewrite
  branch feature/rewrite-1of10 order: 4
  commit tag: "Dev"
  checkout epic/component-rewrite
  merge feature/rewrite-1of10 tag: "Dev / Preview"

  %% Regular feature
  checkout main
  branch feature/2 order: 0
  commit tag: "Dev"
  checkout main
  merge feature/2 tag: "Preview"

  %% Merge into epic latest changes
  checkout epic/component-rewrite
  merge main

  %% Other epic feature
  branch feature/integrate order: 5
  commit tag: "Dev"
  checkout epic/component-rewrite
  merge feature/integrate tag: "Dev / Preview"

  %% Merge back soon!
  checkout main
  merge epic/component-rewrite tag: "Preview"
```

## Releasing

### Simple from main

The simplest way to release is from the _main_ branch. Create a git tag to
trigger a pipeline or start it manually. It should promote the latest build
deployed in the preview environment (after running proper validations) on the
_same commit_ to the production environment.

As part of the release process, the commit deployed must always have a git tag
with the version number.

> [!TIP]  
> This process works well in small projects. It will block any further
> development until the release is done. Consider the
> [release branch workflow](#from-release-branch) to avoid blocking the
> development team.

<!-- Just to have two tips -->

> [!WARNING]  
> The downside of this process is that it usually doesn't allow using a separate
> versioning strategy for _preview_ and _production_ builds without rebuilding
> the same commit in the publishing to production process. The binaries that end
> in production may not be the same tested.

```mermaid
---
config:
  theme: base
  gitGraph:
    showCommitLabel: false
    mainBranchOrder: 3
---

gitGraph LR:
  commit tag: "Preview"

  %% Feature into main
  branch feature/for-vNext order: 2
  commit tag: "Dev"
  commit tag: "Dev"
  checkout main
  merge feature/for-vNext tag: "Preview"

  %% Another feature for next release
  branch feature/for-vNext2 order: 1
  commit tag: "Dev"
  checkout main
  merge feature/for-vNext2 tag: "Preview" tag: "release/vNext"
```

### From release branch

A release branch offers a dedicated process for releasing while not blocking
further product development.

Create a new release from the _main_ branch including the prefix `release/` and
the target version. Some utilities like [GitVersion](https://gitversion.net/)
can even pick the version number from the branch name and automatically assign
it in the product.

Builds from the branch are considered _release candidates_ (RC). They should
have a valid production version number. These binaries should pass a set of
quality assurance tests in an environment close to production.

Once the product is ready, create a git tag or start manually a pipeline to
release to production. This pipeline will _promote_ the artifacts into
production. The commit should end with a git tag with the version number.

After the release, it's safe to remove the release branch. It's always possible
to recreate it from the git tag.

> [!TIP]  
> Promoting to production means taking the exact same binaries used for the
> quality assurance tests and publishing them. It's important to not trigger
> again a build process, so the binaries that end in production are exactly the
> same that passed the tests.

```mermaid
---
config:
  theme: base
  gitGraph:
    showCommitLabel: false
    mainBranchOrder: 3
---

gitGraph LR:
  commit tag: "Preview"

  %% Release branch
  branch release/vNext order: 4
  commit tag: "Preview (RC)" tag: "Production (vNext)"

  %% New feature for the following release
  checkout main
  branch feature/new-feature order: 0
  commit tag: "Dev"
  checkout main
  merge feature/new-feature tag: "Preview"
```

### Release stabilization fixes

As part of the quality assurance process of a _release candidate_ build in the
release branch, new issues found may require fixes. These fixes should be
prioritized for the ongoing release.

The recommendation is to create a [feature](#new-features-and-fixes) branch from
the current release branch. Then merge it back to the release branch, triggering
a new _release candidate_ build.

To get the fix into main there are two strategies:

- After the release, merge the release branch back into the main branch. Use a
  standard merge strategy instead of _git squash_ to preserve the commits of the
  different fixes.
- Create a new feature branch from _main_ and cherry-pick the merge commit of
  the fix (or the commits of the feature fix branch before merging).

```mermaid
---
title: "Git flow"
config:
  theme: base
  gitGraph:
    showCommitLabel: false
    mainBranchOrder: 3
---

gitGraph LR:
  commit tag: "Preview"

  %% Release branch to avoid blocking further work
  branch release/vNext order: 4
  commit tag: "Preview (RC)" %% Fake commit just to have a build tag "from the branch"

  %% Fix after manual test failures
  checkout release/vNext
  branch feature/release-fix order: 5
  commit tag: "Dev"
  checkout release/vNext
  merge feature/release-fix tag: "Preview (RC)" tag: "Production (vNext)"

  %% New feature for the following release
  checkout main
  branch feature/new-feature order: 0
  commit tag: "Dev"
  checkout main
  merge feature/new-feature tag: "Preview"

  %% Merge release branch
  checkout main
  merge release/vNext tag: "Preview"
```

### Patch release

A _patch_ or _hotfix_ release happens when wanting to add new fixes in an
existing released version of the software. The difference with a new release is
that the _main_ branch may already contain commits for a future new feature
release that it shouldn't go into this small release.

In this case, create a new release branch from the _git tag_ of the targeted
release to _fix_. From here, follow the
[above process](#release-stabilization-fixes) adding the required fixes via
feature branches into the patch release branch.

Finally, follow the same release process, git tagging the release and removing
the branch. But, do not merge this hotfix branch into _main_ as it can be
difficult if both branches have diverged a lot. Bring the fixes into main
manually or via cherry-picks feature branches.

```mermaid
---
title: "Git flow"
config:
  theme: base
  gitGraph:
    showCommitLabel: false
    mainBranchOrder: 3
---

gitGraph LR:
  commit tag: "Preview"

  %% Release branch to avoid blocking further work
  branch release/vNext order: 4
  commit tag: "Preview (RC)" %% Fake commit just to have a build tag "from the branch"

  %% Breaking feature not for the next release
  checkout main
  branch feature/new-feature order: 1
  commit tag: "Dev"
  checkout main
  merge feature/new-feature tag: "Preview"

  %% Fix after manual test failures
  checkout release/vNext
  branch feature/release-fix order: 5
  commit tag: "Dev"
  checkout release/vNext
  merge feature/release-fix tag: "Preview (RC)" tag: "Production (vNext)"

  %% Merge release branch
  checkout main
  merge release/vNext tag: "Preview"

  %% Another development
  branch feature/new-feature2 order: 0
  commit tag: "Dev"
  checkout main
  merge feature/new-feature2 tag: "Preview"

  %% Release hot-fix
  checkout "release/vNext"
  branch release/vNext.patch order: 5
  commit tag: "Preview (RC)" %% Fake commit just to have a build tag "from the branch"

  branch feature/fix-hotfix order: 5
  commit tag: "Dev"
  checkout release/vNext.patch
  merge feature/fix-hotfix tag: "Preview (RC)" tag: "Production (vNext.1)"

  %% Merge hotfix to main as cherry-picks from now on
  checkout main
  commit type: HIGHLIGHT tag: "cherry-pick hotfix (via PR) - Preview"
```

## Complete diagram

```mermaid
---
title: "Git flow"
config:
  theme: base
  gitGraph:
    showCommitLabel: false
    mainBranchOrder: 3
---

gitGraph LR:
  commit tag: "Preview"

  %% Feature into main
  branch feature/for-vNext order: 2
  commit tag: "Dev"
  commit tag: "Dev"
  checkout main
  merge feature/for-vNext tag: "Preview -> Prod (vNext)"

  %% Another feature for next release
  branch feature/for-vNext2 order: 1
  commit tag: "Dev"
  checkout main
  merge feature/for-vNext2 tag: "Preview"

  %% Release branch to avoid blocking further work
  branch release/vNext2 order: 4

  %% Breaking feature not for the next release
  branch feature/breaking order: 0
  commit tag: "Dev"
  checkout main
  merge feature/breaking tag: "Preview"

  %% Fix after manual test failures
  checkout release/vNext2
  branch feature/release-fix order: 5
  commit tag: "Dev"
  checkout release/vNext2
  merge feature/release-fix tag: "Preview (RC) -> Production (vNext2)"

  %% Merge release branch
  checkout main
  merge release/vNext2 tag: "Preview"

  %% Another development
  branch feature/for-vNext3 order: 0
  commit tag: "Dev"
  checkout main
  merge feature/for-vNext3 tag: "Preview"

  %% Release hot-fix
  checkout "release/vNext2"
  branch release/hotfix-vNext2 order: 5

  branch feature/fix-hotfix-vNext2 order: 5
  commit tag: "Dev"
  checkout release/hotfix-vNext2
  merge feature/fix-hotfix-vNext2 tag: "Preview (RC) -> Production (vNext2-hotfix1)"

  %% Merge hotfix to main as cherry-picks from now on
  checkout main
  commit type: HIGHLIGHT tag: "cherry-pick hotfix (via PR) - Preview"
```
