import fs from 'node:fs';
import { getDocument } from '../review-tools/node_modules/pdfjs-dist/legacy/build/pdf.mjs';
const data = new Uint8Array(fs.readFileSync('C:/Users/Admin/Downloads/SWD392 - Report 2 (SDS) - SE1972-NET - Group 1 _v1.0.docx.pdf'));
const doc = await getDocument({ data, useSystemFonts: true }).promise;
let text = '';
for (let i = 1; i <= doc.numPages; i++) {
  const page = await doc.getPage(i);
  const content = await page.getTextContent();
  text += '\n--- PAGE ' + i + ' ---\n' + content.items.map(x => x.str + (x.hasEOL ? '\n' : ' ')).join('');
}
fs.writeFileSync(new URL('./sds.txt', import.meta.url), text);
console.log('Extracted pages:', doc.numPages);
