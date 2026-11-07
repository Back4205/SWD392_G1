# Cleanup plan — 07/11/2026

## Scope

Inner repository `SWD392_G1`, current baseline `bf098a3`. User reports inbound flow working and requests removal of redundant code. Keep all current UI/product/font changes and untracked seed/maintenance files.

## Acceptance criteria

- Remove only code proven unused by whole-inner-repository references.
- Preserve HTTP routes/JSON, workflow/state, transaction/version checks and database schema.
- Preserve classes and operations used to explain SDS, including generic repository update and serial lookup.
- Backend build and frontend lint/build remain clean; run focused regression without invoking development-data maintenance tests.

## Reviewed plan

Independent backend scout confirmed five legacy repository methods have no consumers and are superseded by active-data validation or generic update. Frontend scout covers unused exports and import coupling.

- [x] Remove legacy Exists checks and duplicate UpdateAsync from Inbound repository/interface.
- [x] Remove redundant imports in those files and applicable DTOs.
- [x] Remove unused frontend session fetch helper, unnecessary error-class re-export and three unreachable CSS blocks confirmed by scout.
- [x] Independent review and focused verification.

## Final verification

- Backend API/test build in separate temporary OutputPath: 0 warnings, 0 errors. Normal output was locked by user's running API; no application stopped.
- 29 focused tests passed: 26 Auth/entity cases, full Goods Receipt HTTP workflow, and both approval/rejection Inbound HTTP cases. The earlier HTTP 500 did not reproduce; original cause remains unconfirmed.
- Frontend lint and TypeScript/Vite production build passed.
- Independent reviewer found no concrete behavior/state/API regression in the cleanup.
- Development-data maintenance test excluded by explicit test allowlist.

## Baseline verification note

Earlier full suite reached 134 passed before final controller extraction. Latest full rerun before cleanup ended 133 passed / 1 failed: DocumentReceiptDecisionTests.InboundHttpApprovesWithoutNoteOrRequiresReasonAndPurchasingReadsDecision(false) returned 500 after 34 seconds. Cause has not yet been established; do not label current full suite passing. No cleanup source edits existed at that point.

The newly present untracked FixVietnameseFontEncodingTest edits the development database and source mapping, so routine verification must exclude it. It is preserved, not executed or deleted in this cleanup.
