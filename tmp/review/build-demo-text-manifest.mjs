import fs from 'node:fs';
const source = fs.readFileSync('SWD392_G1/seed_data.sql', 'utf8');
function values(text) {
  const parts = []; let part = '', quoted = false;
  for (let i = 0; i < text.length; i++) {
    const c = text[i];
    if (c === "'") {
      part += c;
      if (quoted && text[i + 1] === "'") { part += text[++i]; continue; }
      quoted = !quoted;
    } else if (c === ',' && !quoted) { parts.push(part.trim()); part = ''; }
    else part += c;
  }
  parts.push(part.trim());
  return parts.map(s => /^(?:N)?'/.test(s) ? s.replace(/^N?'/, '').replace(/'$/, '').replaceAll("''", "'") : s);
}
const entries = [];
const add = (table, keyColumn, keyValue, column, correctValue) => entries.push({ table, keyColumn, keyValue: String(keyValue), column, correctValue });
const definitions = {
  Supplier: ['SupplierCode', ['SupplierName', 'ContactPerson', 'Address']],
  Product: ['ProductCode', ['ProductName']],
  ProductVariant: ['SKU', ['VariantName', 'Specs']],
  Category: ['CategoryName', ['CategoryName']]
};
for (const match of source.matchAll(/INSERT INTO (\w+) \(([^)]+)\)\s*VALUES \(([\s\S]*?)\);/g)) {
  const def = definitions[match[1]]; if (!def) continue;
  const columns = match[2].split(',').map(x => x.trim()); const data = values(match[3]);
  for (const column of def[1]) add(match[1], def[0], data[columns.indexOf(def[0])], column, data[columns.indexOf(column)]);
}
add('Warehouse', 'WarehouseCode', 'WH-HN-01', 'WarehouseName', 'Kho Tổng Hà Nội');
add('Warehouse', 'WarehouseCode', 'WH-HN-01', 'Address', 'Số 1 Trịnh Văn Bô, Nam Từ Liêm, Hà Nội');
add('StorageSectionType', 'Id', 1, 'Description', 'Khu vực lưu kho tiêu chuẩn');
add('StorageSection', 'Id', 1, 'SectionName', 'Khu Nhập & Lưu kho A');
for (let i = 1; i <= 10; i++) {
  const suffix = String(i).padStart(2, '0');
  add('StorageBin', 'BinCode', `BIN-A1-${suffix}`, 'BinName', `Vị trí lưu kho A1-${suffix}`);
}
for (const [key, name] of Object.entries({
  admin: 'Quản trị viên hệ thống', manager: 'Quản lý kho (Nguyễn Văn Quản)',
  purchasing: 'Trưởng phòng Mua hàng (Trần Thị Mua)', staff: 'Nhân viên kho (Lê Văn Kho)',
  sales: 'Nhân viên bán hàng (Phạm Văn Bán)'
})) add('User', 'UserName', key, 'FullName', name);
if (entries.length !== 99 || entries.some(x => !x.correctValue || x.correctValue.startsWith('@'))) throw Error('Unexpected seed manifest');
fs.writeFileSync('SWD392_G1/scripts/demo-text-repairs.json', JSON.stringify(entries, null, 2) + '\n');
console.log(`Prepared ${entries.length} source-backed text mappings`);
