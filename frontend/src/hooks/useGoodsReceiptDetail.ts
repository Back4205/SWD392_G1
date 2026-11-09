import { useEffect, useRef, useState } from "react";
import {
  addReceiptSerial, approveGoodsReceipt, getGoodsReceipt, getReceiptBins,
  rejectGoodsReceipt, saveGoodsReceipt, submitGoodsReceipt,
} from "../features/goods-receipts/goodsReceipt.api";
import type { GoodsReceipt, ReceiptBin, ReceiptSession } from "../features/goods-receipts/goodsReceipt.types";
import { isStaleReceipt, receiptErrorMessage } from "../features/goods-receipts/receipt.helpers";

function getSavedBins(receipt: GoodsReceipt) {
  const values: Record<number, number | null> = {};
  for (const item of receipt.items) values[item.id] = item.binId;
  return values;
}

export default function useGoodsReceiptDetail(receiptId: number, session: ReceiptSession, onBusyChange: (busy: boolean) => void) {
  const [receipt, setReceipt] = useState<GoodsReceipt | null>(null);
  const [bins, setBins] = useState<ReceiptBin[]>([]);
  const [binChoices, setBinChoices] = useState<Record<number, number | null>>({});
  const [selectedItemId, setSelectedItemId] = useState(0);
  const [serialCode, setSerialCode] = useState("");
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [binError, setBinError] = useState("");
  const [notice, setNotice] = useState("");
  const [decision, setDecision] = useState<"approve" | "reject" | null>(null);
  const [reloadKey, setReloadKey] = useState(0);
  const scanInput = useRef<HTMLInputElement>(null);
  const writeInProgress = useRef(false);

  useEffect(() => {
    let active = true;
    async function loadDetail() {
      setLoading(true);
      setError("");
      setBinError("");
      try {
        const result = await getGoodsReceipt(receiptId);
        if (active) {
          setReceipt(result);
          setBinChoices(getSavedBins(result));
          setSelectedItemId(result.items[0]?.id || 0);
        }
        // Phiếu lịch sử vẫn xem được khi không tải được danh sách bin (ví dụ kho ngừng hoạt động).
        try {
          const locations = await getReceiptBins(result.warehouseId);
          if (active) setBins(locations);
        } catch (lookupError) {
          if (active) setBinError(receiptErrorMessage(lookupError));
        }
      } catch (err) {
        if (active) setError(receiptErrorMessage(err));
      } finally {
        if (active) setLoading(false);
      }
    }
    void loadDetail();
    return () => { active = false; };
  }, [receiptId, session.userId, reloadKey]);

  const editable = receipt?.status === "Draft" && session.role === "Warehouse Staff" && receipt.receivedByUserId === session.userId;
  const canDecide = receipt?.status === "PendingApproval" && session.role === "Manager";
  const selectedItem = receipt?.items.find(item => item.id === selectedItemId);
  const quantity = receipt?.items.reduce((total, item) => total + item.receivedQty, 0) || 0;
  const placementComplete = quantity > 0 && receipt?.items.every(item => item.receivedQty === 0 || item.binId !== null);
  const binsChanged = receipt?.items.some(item => (binChoices[item.id] ?? null) !== item.binId) || false;

  function beginWrite() {
    // Ref chặn cả trường hợp máy scan gửi Enter liên tiếp trước khi React kịp render.
    if (writeInProgress.current) return false;
    writeInProgress.current = true;
    onBusyChange(true);
    setError("");
    setNotice("");
    return true;
  }

  function finishWrite() {
    writeInProgress.current = false;
    onBusyChange(false);
  }

  async function handleWriteError(err: unknown) {
    setError(receiptErrorMessage(err));
    if (isStaleReceipt(err)) {
      setDecision(null);
      try {
        const latest = await getGoodsReceipt(receiptId);
        setReceipt(latest);
        setBinChoices(getSavedBins(latest));
      } catch (reloadError) {
        setError(`Chưa tải lại được phiếu mới. ${receiptErrorMessage(reloadError)}`);
      }
    }
    // Duplicate cũng là 409, nhưng không phải stale: giữ dòng scan và lựa chọn bin.
  }

  async function handleScanForItem(itemId: number, code: string) {
    const trimmed = code.trim();
    if (!receipt || !trimmed || !beginWrite()) return;
    const targetItem = receipt.items.find(item => item.id === itemId);
    if (!targetItem) {
      finishWrite();
      return;
    }
    try {
      await addReceiptSerial(receipt.id, targetItem.id, receipt.version, trimmed, targetItem.sku);
      const latest = await getGoodsReceipt(receipt.id);
      setReceipt(latest);
      setSerialCode("");
      setNotice(`Đã lưu serial [${trimmed}] cho thiết bị của sản phẩm ${targetItem.productName || targetItem.sku}.`);
    } catch (err) {
      await handleWriteError(err);
    } finally {
      finishWrite();
      setTimeout(() => scanInput.current?.focus(), 0);
    }
  }

  async function handleScan() {
    if (!selectedItem || !serialCode) return;
    await handleScanForItem(selectedItem.id, serialCode);
  }

  async function handleSave() {
    if (!receipt || !beginWrite()) return;
    try {
      const items = receipt.items.map(item => ({ itemId: item.id, binId: binChoices[item.id] ?? null }));
      const latest = await saveGoodsReceipt(receipt.id, receipt.version, items);
      setReceipt(latest);
      setBinChoices(getSavedBins(latest));
      setNotice("Đã lưu phiếu nháp. Tồn kho chưa thay đổi.");
    } catch (err) {
      await handleWriteError(err);
    } finally {
      finishWrite();
    }
  }

  async function handleSubmit() {
    if (!receipt || !beginWrite()) return;
    try {
      // Bước 1: lưu bin thành công; bước 2: gửi duyệt bằng Version vừa được server trả về.
      const items = receipt.items.map(item => ({ itemId: item.id, binId: binChoices[item.id] ?? null }));
      const saved = await saveGoodsReceipt(receipt.id, receipt.version, items);
      setReceipt(saved);
      setBinChoices(getSavedBins(saved));
      const pending = await submitGoodsReceipt(saved.id, saved.version);
      setReceipt(pending);
      setNotice("Đã gửi Manager duyệt. Phiếu được khóa chỉnh sửa; tồn kho chưa thay đổi.");
    } catch (err) {
      await handleWriteError(err);
    } finally {
      finishWrite();
    }
  }

  async function handleDecision(note: string) {
    if (!receipt || !decision || !beginWrite()) return;
    try {
      const result = decision === "approve"
        ? await approveGoodsReceipt(receipt.id, receipt.version)
        : await rejectGoodsReceipt(receipt.id, receipt.version, note);
      setReceipt(result);
      setDecision(null);
      setNotice(result.status === "Approved" ? "Đã duyệt phiếu. Tồn kho, serial và tiến độ Inbound đã được cập nhật." : "Đã từ chối phiếu và lưu lý do. Tồn kho không thay đổi.");
    } catch (err) {
      await handleWriteError(err);
      setDecision(null); // Yêu cầu xem lại dữ liệu trước khi đưa ra quyết định tiếp.
    } finally {
      finishWrite();
    }
  }

  return {
    receipt, bins, binChoices, setBinChoices, selectedItemId, setSelectedItemId,
    serialCode, setSerialCode, loading, error, binError, notice, decision, setDecision,
    scanInput, editable, canDecide, quantity, placementComplete, binsChanged,
    reload: () => setReloadKey(value => value + 1),
    handleScan, handleScanForItem, handleSave, handleSubmit, handleDecision,
  };
}
