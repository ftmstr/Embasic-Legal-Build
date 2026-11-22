# CMK Curriculum Implementation Plan

> **For agentic workers:** Execute in the current session with superpowers:executing-plans. Steps use checkbox syntax for tracking.

**Goal:** Sol menüden erişilen kapsamlı ve etkileşimli CMK öğrenme kategorileri.
**Architecture:** CmkCurriculum kategorileri ve dersleri tek doğrulanabilir çekirdek katalogda tutar. WPF aynı öğrenme oturumunu kategori/konu değişimlerinde paylaşır; kaynak statüsü ayrı kalır.
**Tech Stack:** Existing .NET 10 / WPF / xUnit, no new dependencies.
**Spec:** docs/superpowers/specs/2026-11-22-cmk-curriculum.md

## Global Constraints

- Preserve legacy topic IDs, quizzes and session marks.
- Official current law NOT_VERIFIED; no invented source receipt or legal authorization.
- Draft branch only; no main merge or release.

## Review Focus

- Every category must open a valid topic without stale selection.
- Topic changes must preserve independent checklist marks and global quiz totals.
- Every main numbered article locator 1–335 must be represented; insertions and transitional provisions remain explicitly unverified.
- Long Turkish text must wrap in sidebar, quizzes and expanding diagrams.
- User-provided DOC link must remain directly visible and copyable.

### Task 1: Curriculum

**Files:** Create Core/CmkCurriculum.cs and Tests/Tur026CmkCurriculumTests.cs; extend CmkLearningCatalog.Find.
**Interfaces:** CmkCurriculum.Categories; All; FindCategory(string); Explain(string); CmkLearningCatalog.Find/Assess unchanged signatures.
- [ ] Build 13 categories with all-numbered-article reading coverage, original explanatory lessons and topic-specific quizzes.
- [ ] Test exact category ownership, unique IDs, retained legacy content, article coverage and unverified sources.

### Task 2: WPF integration

**Files:** MainViewModel.cs, CmkLearningHubViewModel.cs, App.xaml, MainWindow.xaml.
**Interfaces:** OpenCategory(string); Categories; SelectedCategory; Topics; Lesson.
- [ ] Add category routes and reset topic selection on category changes; retain RAM session.
- [ ] Add explanation tab and expandable flow cards; wrap long Turkish labels.
- [ ] Build and test in actual Windows CI; collect manual UI acceptance list without claiming it ran.

### Task 3: Delivery

**Files:** windows-ci.yml; docs/TUR026-CMK-CURRICULUM.md.
- [ ] Publish draft PR against TUR025 branch and verify exact source Windows CI.
- [ ] Download original Windows artifact, validate hashes and save deliverable with truthful source limitation.
