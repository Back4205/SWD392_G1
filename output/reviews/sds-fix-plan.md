# SDS alignment implementation record

## Scope and decision

Inner repository `SWD392_G1`, baseline `11c376d`, branch `Bacsk/goods-receipt`.
User explicitly requested implementation after review and chose: preserve old sequence business rules; update State diagrams to agree. Local code/doc changes only; no commit, push, deployment or mail delivery.

## Diagnosis

- Symptom: repository lifelines and entity operations shown in SDS had no implementation/call sites; Services queried DbContext directly.
- Reproduction: trace Login, Inbound Detail/Commitment/Review, Receipt Scan/Approval from Controller to persistence; compare Repositories folder and entity declarations against class diagrams.
- Expected: actual Controller → Service → Repository → DbContext with entity operations used, while preserving receipt/inbound transaction semantics.
- Cause: baseline `11c376d` implements business flow in Services but omits persistence/domain layers shown by SDS; auth minimum password length 12 and combined inactive response diverge from SD07/SD09.
- Exposed by: user's demo requires code traceability to supplied class/sequence and newly supplied architecture/state, rather than UI success alone.
- Affected paths: Auth/session/password, Inbound create/list/detail/commitment/review/lookups, Receipt create/list/detail/scan/submit/decision, Stock, receiving progress and detail-page hooks.

## Progress

- [x] Scout code, recent commits, existing test coverage and specification conflicts.
- [x] Resolve conflict with user: retain sequence rules, correct State documentation.
- [x] Add real generic and domain repositories; invoke domain operations.
- [x] Route Auth and Inbound through repositories and preserve shared transaction behavior.
- [x] Extract detail frontend read/write state into hooks.
- [x] Align SD07 inactive branch and SD09 6-character boundary.
- [x] Add state/quantity/stock and auth regression tests.
- [x] Full SQL/HTTP regression: 134 passed after repository/guard changes.
- [x] Frontend lint/build and independent review.
- [ ] Finish Controller lookup extraction and final regression.
- [ ] Publish corrected State/mapping/demo guide and final verification record.

## Contract notes

No migration/schema change. Existing HTTP routes and payloads retained. Intentional auth behavior changes: inactive credentials with correct password get a distinct code; new passwords allow 6–128 characters per sequence. Rejected/Canceled/Completed remain terminal; invalid serial/SKU returns validation error without rejecting the draft. Product/Supplier full CRUD and SMTP integration are outside the implemented inbound workflow.
