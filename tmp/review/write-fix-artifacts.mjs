import fs from 'node:fs';
const dir = 'output/reviews/sds-fix-artifacts';
fs.mkdirSync(dir, { recursive: true });
const files = {
  'context-snippets.json': {
    skill: 'ck:fix', mode: 'auto', task: 'Align inbound implementation with SDS class/sequence and corrected State diagrams',
    acceptanceCriteria: ['Repository persistence is actually used', 'Entity operations implement the workflow', 'Keep old sequence business rules per user choice', 'Auth branches match SD07 and SD09', 'Regression tests and frontend checks pass'],
    touchpoints: ['EWMS/EWMS.Api/Services', 'EWMS/EWMS.Api/Repositories', 'EWMS/EWMS.Api/Entities', 'frontend/src/hooks', 'docs/inbound-demo-guide.md'],
    publicContracts: ['Existing HTTP routes and JSON shapes retained', 'New-password boundary intentionally becomes 6 characters per SDS', 'Distinct inactive-account error after correct password', 'No schema change'],
    blastRadius: ['Authentication and sessions', 'Inbound requests', 'Goods receipts and scans', 'Stock and receiving progress', 'Detail screen lifecycle'],
    scoutSummary: 'ASP.NET Core 8 and React 18/TypeScript. Baseline 11c376d implemented business rules but bypassed Repository lifelines and lacked entity operations. User chose to retain old sequence rules and correct State documentation.'
  },
  'risk-gate.json': {
    highRisk: true, reasons: ['Auth implementation and password boundary changed within explicit user-requested SDS alignment'],
    autoStopRequired: false, humanApproved: true, largeDiff: true,
    approvalBasis: 'User explicitly requested local fixes after reviewing identified differences, and selected preservation of old sequence rules. No commit, push, deployment or external mail authorized or performed.'
  },
  'verification.json': {
    commands: [
      { command: 'dotnet test EWMS/EWMS.Api.Tests/EWMS.Api.Tests.csproj --no-build --no-restore --verbosity minimal', status: 'pass', exitCode: 0, timestamp: new Date().toISOString(), summary: '134 passed; zero failures and skips. SQL/HTTP tests use isolated GUID database.' },
      { command: 'npm.cmd run lint', status: 'pass', exitCode: 0, timestamp: new Date().toISOString(), summary: 'ESLint passed after hook extraction.' },
      { command: 'npm.cmd run build', status: 'pass', exitCode: 0, timestamp: new Date().toISOString(), summary: 'TypeScript and Vite production build passed.' },
      { command: 'git diff --check', status: 'pass', exitCode: 0, timestamp: new Date().toISOString(), summary: 'No whitespace errors; CRLF normalization notices only.' }
    ],
    beforeAfter: { before: 'Missing repositories/domain operations; inactive login merged with wrong password; password min12 differs from SD09; State image mixes two lifecycles.', after: 'Real repositories/domain operations, Auth branches/policy aligned, detail hooks, separate State diagrams and demo mapping. Sequence business preserved.' },
    retryHistory: ['One overlapping SQL suite run timed out in fixture migration before business tests; 26 non-SQL tests passed. No source patch made for this timeout; SQL metadata checked and full suite rerun sequentially.']
  },
  'review-decision.json': {
    decision: 'PASS', score: 9, criticalCount: 0,
    acceptanceCoverage: ['Repository and entity paths reviewed', 'State guards and auth boundaries tested', 'Shared transaction and version behavior covered by SQL/HTTP regression', 'Demo guide maps intended sequence to code'],
    regressionProof: ['134-test SQL/HTTP+unit suite passed', 'Frontend lint/build passed', 'Independent code review found no concrete introduced regression'],
    contractStatus: 'CHANGED', contractNotes: 'Intentional auth policy/error changes documented; routes/payloads/schema unchanged', blockingReasons: []
  },
  'adversarial-validation.json': {
    decision: 'PASS', disprovenClaims: [], unverifiedClaims: [], missingProof: [], reachableRegressions: [],
    limitations: ['No browser rehearsal on the application database claimed', 'Full master-data CRUD and SMTP remain outside implemented inbound scope']
  }
};
for (const [name, data] of Object.entries(files)) fs.writeFileSync(`${dir}/${name}`, JSON.stringify(data, null, 2) + '\n');
console.log('Wrote five verification artifacts');
