import type { InboundRequestDetailItem } from "./inbound.api";

// Một dòng là một serial. Luôn giữ chuỗi để không mất số 0 đầu của IMEI.
export function parseSerialLines(value: string): string[] {
  return value.split(/\r?\n/).map(line => line.trim()).filter(line => line.length > 0);
}

export function manifestInputErrors(items: InboundRequestDetailItem[], serialTexts: Record<number, string>) {
  const errors: Record<number, string[]> = {};
  const usedSerials = new Map<string, { itemId: number; sku: string }>();
  for (const item of items) {
    errors[item.id] = [];
    const serials = parseSerialLines(serialTexts[item.id] || "");
    if (serials.length !== item.expectedQty) {
      errors[item.id].push(`Cần ${item.expectedQty} serial, hiện có ${serials.length}.`);
    }
    for (let index = 0; index < serials.length; index++) {
      const serial = serials[index];
      if (!/^[A-Za-z0-9._-]{1,100}$/.test(serial)) {
        errors[item.id].push(`Dòng ${index + 1}: serial phải có 1–100 ký tự chữ, số, dấu chấm, gạch dưới hoặc gạch ngang.`);
      }
      // Database không phân biệt hoa/thường khi kiểm tra trùng; UI kiểm tra tương ứng.
      const key = serial.toUpperCase();
      const used = usedSerials.get(key);
      if (used) {
        errors[item.id].push(`Dòng ${index + 1}: serial "${serial}" đã xuất hiện ở SKU ${used.sku}.`);
        if (used.itemId !== item.id) errors[used.itemId].push(`Serial "${serial}" cũng xuất hiện ở SKU ${item.sku}.`);
      } else usedSerials.set(key, { itemId: item.id, sku: item.sku });
    }
  }
  return errors;
}
