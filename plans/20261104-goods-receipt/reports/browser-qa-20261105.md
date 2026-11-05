# React Goods Receipt — browser QA 2026-11-05

Status: passed. Real Chrome headless, isolated profile; real Vite `127.0.0.1:5173` and API `127.0.0.1:5085`. No fake success responses. Database is dedicated `EWMS_GoodsReceipt_Development`.

## Verified flows

- Created fresh Inbound 6 through existing Inbound API, then created Receipt 1 from scoped React lookup.
- Scanned 8 devices through React. Invalid format and duplicate serial did not increment count. Choosing a bin then scanning preserved the unsaved selection.
- An external API scan changed the Version; UI save displayed stale conflict, reloaded scans, and did not silently retry placement.
- Saved draft, reloaded the browser, and verified 8 scans and bin persisted.
- Staff Submit locked edit controls and left official received progress at 0.
- Manager UI confirmation approved 8 devices: progress 8 received / 2 remaining / PartiallyReceived.
- Staff created Receipt 2 for the remaining 2, scanned/submitted, then Manager approved: progress 10 / 0 / Completed. Completed inbound disappeared from create lookup.
- New Inbound 7 / Receipt 3 tested Reject: whitespace reason blocked in UI; Vietnamese reason persisted; no progress or official serial generated. Manager could not open the creation form; rejected receipt was read-only.
- New Inbound 8 / Receipt 4 tested rapid double submit: one real scan HTTP request and one increment. Actual focused-input keyboard Enter added the next serial and focus returned to scan input.
- Hash status/warehouse/date filters survived real reload; browser Back restored detail. Skip link focused main content without changing hash.
- Date filter kept calendar dates in URL and sent Bangkok-local midnight as UTC API boundary.
- Existing Inbound UI still loaded real requests.
- Offline emulation displayed an error and Retry; restoring the connection and retrying loaded the list.
- Injecting failure only for optional bin lookup still displayed real historical receipt, serials and Bin ID fallback.

## Layout/runtime checks

- Desktop 1440 × 1000 and mobile 375 × 812 inspected.
- Found mobile list overflow caused by an absolute screen-reader-only table label escaping the scroll region. Parent fixed `.gr-table-scroll { position: relative; }`; retest gave documentElement clientWidth = scrollWidth = innerWidth = 375.
- Mobile detail had no page overflow; visible action controls were at least 44px high. Reduced-motion media preference enabled.
- Final browser console: no error messages. Intentional offline test previously produced only expected `ERR_INTERNET_DISCONNECTED` resource errors.

## Independent SQL confirmation

Read-only SQL after UI operations: Bin 1 / Variant 1 OnHandQty = 10, ReservedQty = 0; exactly 10 receipt movements for approved Receipts 1/2; Receipt 3 generated 0 official serials.

Seed Inbounds 1/2 were not consumed. The demo database now additionally contains QA Inbounds 6/7/8 and Receipts 1–4; Receipt 4 remains Draft with 2 scans and bin 1 for continued manual practice. No data reset or deletion was performed.

## Evidence

Ignored artifacts under `frontend/node_modules/.cache/receiving-qa/`:

- `browser-report.json` — assertion results and fixture IDs.
- `part1.js` … `part4.js` plus focused check scripts — executed QA actions.
- `detail-desktop.png`, `detail-mobile.png`, `list-mobile.png` — screenshots of real UI.

Browser left open on page 2: `http://127.0.0.1:5173/#/goods-receipts/4`, Staff, desktop 1440 × 1000, online, no emulation. Parent owns final server/browser teardown. No production source files were changed by this tester.
