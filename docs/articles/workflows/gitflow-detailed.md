# Git flow in detail

The [parent page](./gitflow.md) offers an overview of the proposed _Git flow_.
This document describes in detail each use case during development and
releasing.

## Developing

### New features and fixes

Developers implements new features and bug fixes from a new branch each time.
The feature branches have the prefix `feature/` and start from the `main` (or
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

### Large breaking feature

Large features may take several work batches to implement, breaking or making
the product unstable for a period of time. For these cases, consider having a
parallel branch to _main_ to merge related features. It allows to break a large
work into smaller features, while not impacting other developers or the main
product development.

It also allows to configure different temporal branch policies, for instance
disable quality gates or even building some components, while the new work is
being implemented.

Depending on the work, consider configuring the _epic_ branch to build and
deploy on a separate environment for validation.

> [!TIP]  
> Prioritize merging the _epic_ branch as soon as possible. The maintaince cost
> increases quickly and it can be tricky to merge back later.

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
  branch feature/work-start order: 4
  commit tag: "Dev"
  checkout epic/component-rewrite
  merge feature/work-start tag: "Preview"

  %% Regular feature
  checkout main
  branch feature/2 order: 0
  commit tag: "Dev"
  checkout main
  merge feature/2 tag: "Preview"

  %% Other epic feature
  checkout epic/component-rewrite
  branch feature/integrate order: 5
  commit tag: "Dev"
  checkout epic/component-rewrite
  merge feature/integrate tag: "Preview"

  %% Merge back soon!
  checkout main
  merge epic/component-rewrite tag: "Preview"
```

## Releasing

### Simple from main

TODO

### From release branch

TODO

### Release stabilization fixes

TODO

### Patch release

TODO

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
