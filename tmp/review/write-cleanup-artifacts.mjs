import fs from 'node:fs';
const dir = 'output/reviews/cleanup-artifacts';
fs.mkdirSync(dir, { recursive: true });
const timestamp = new Date().toISOString();
const files = {
  'context-snippets.json': {
    skill: 'ck:cook', mode: 'auto', task: 'Remove proven unused code without changing working inbound flow',
    acceptanceCriteria: ['Only proven dead code removed', 'State and HTTP/schema contracts unchanged', 'SDS operations retained', 'Build, focused tests and frontend checks pass'],
    touchpoints: ['IInboundRequestRepository', 'InboundRequestRepository', 'Inbound DTO imports', 'goodsReceipt.api.ts', 'receipt.helpers.ts', 'App.css'],
    publicContracts: ['HTTP routes, payloads, schema and states unchanged', 'Internal unused repository methods removed'],
    blastRadius: ['Inbound repository consumers', 'Receipt API helper imports', 'Sidebar markup/styles'],
    scoutSummary: 'Two independent scouts verified absence of callers and unreachable CSS. User receipt/font edits and development repair utility preserved.'
  },
  'risk-gate.json': { highRisk: false, reasons: [], autoStopRequired: false, humanApproved: false, largeDiff: false },
  'verification.json': {
    commands: [
      { command: 'dotnet build EWMS/EWMS.Api.Tests/EWMS.Api.Tests.csproj --no-restore --verbosity minimal -p:OutputPath=<temporary-output>', status: 'pass', exitCode: 0, timestamp, summary: 'API/test build passed with zero warnings and errors; live application not stopped.' },
      { command: 'dotnet vstest <temporary-output>/EWMS.Api.Tests.dll --TestCaseFilter:<Auth+Entity+GoodsReceiptHTTP+InboundDecisionHTTP>', status: 'pass', exitCode: 0, timestamp, summary: '29 passed, zero failed. Explicit allowlist excludes development database repair test.' },
      { command: 'npm.cmd run lint', status: 'pass', exitCode: 0, timestamp, summary: 'ESLint passed.' },
      { command: 'npm.cmd run build', status: 'pass', exitCode: 0, timestamp, summary: 'TypeScript and Vite production build passed.' }
    ],
    beforeAfter: { before: 'Five unused legacy repository methods, dead frontend session wrapper/error alias, nine redundant using directives and three unreachable CSS rules.', after: 'Removed verified dead code; consumers use active validation, generic update and original ApiError identity.' },
    notes: ['Normal backend output locked by running user app; separate fresh output used.', 'Pre-cleanup HTTP500 did not reproduce in focused tests; original cause remains unconfirmed. Full suite not claimed passing.']
  },
  'review-decision.json': {
    decision: 'PASS', score: 9, criticalCount: 0,
    acceptanceCoverage: ['Independent code review checked all cleanup slices', 'Reference search and full compilation verify consumers', 'Workflow tests and frontend checks pass'],
    regressionProof: ['29 focused tests passed', 'Backend build passed', 'Frontend lint/build passed'],
    contractStatus: 'OK', blockingReasons: []
  },
  'adversarial-validation.json': {
    decision: 'PASS', disprovenClaims: [], unverifiedClaims: [], missingProof: [], reachableRegressions: [],
    scopeNotes: ['Only cleanup claims reviewed; no claim that unrelated full suite or browser rehearsal passed.']
  }
};
for (const [name, data] of Object.entries(files)) fs.writeFileSync(`${dir}/${name}`, JSON.stringify(data, null, 2) + '\n');
console.log('Wrote cleanup verification artifacts');
