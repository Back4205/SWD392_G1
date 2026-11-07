import fs from 'node:fs';
import { createCanvas } from '../review-tools/node_modules/@napi-rs/canvas/index.js';
import { getDocument } from '../review-tools/node_modules/pdfjs-dist/legacy/build/pdf.mjs';
const data = new Uint8Array(fs.readFileSync('C:/Users/Admin/Downloads/SWD392 - Report 2 (SDS) - SE1972-NET - Group 1 _v1.0.docx.pdf'));
const doc = await getDocument({ data, useSystemFonts: true }).promise;
for (const i of [23,24,26,31,32,37,39,40]) {
  const page = await doc.getPage(i);
  const viewport = page.getViewport({ scale: 2.5 });
  const canvas = createCanvas(Math.ceil(viewport.width), Math.ceil(viewport.height));
  await page.render({ canvasContext: canvas.getContext('2d'), viewport }).promise;
  fs.writeFileSync(new URL('./page-' + i + '.png', import.meta.url), canvas.toBuffer('image/png'));
}
console.log('Rendered SDS pages');
