
    using EWMS.Api.DTOs.InboundRequests;
    using EWMS.Api.Entities;
    using EWMS.Api.Interfaces;

    namespace EWMS.Api.Services
    {
        public class InboundRequestService : IInboundRequestService
        {
            private readonly IInboundRequestRepository _repository;

            public InboundRequestService(
                IInboundRequestRepository repository)
            {
                _repository = repository;
            }

            // Task 1: Create Inbound Request
            public async Task<int> CreateAsync(
                CreateInboundRequestDto dto,
                int requestedByUserId)
            {
                // 1. Validate Warehouse
                if (dto.WarehouseId <= 0 ||
                    !await _repository.WarehouseExistsAsync(dto.WarehouseId))
                {
                    throw new ArgumentException(
                        "Warehouse does not exist.");
                }

                // 2. Validate User
                if (requestedByUserId <= 0 ||
                    !await _repository.UserExistsAsync(requestedByUserId))
                {
                    throw new ArgumentException(
                        "Requested user does not exist.");
                }

                // 3. Validate Items
                if (dto.Items == null || dto.Items.Count == 0)
                {
                    throw new ArgumentException(
                        "At least one product is required.");
                }

                // 4. Check duplicate ProductVariant
                if (dto.Items.Select(x => x.ProductVariantId)
                    .Distinct().Count() != dto.Items.Count)
                {
                    throw new ArgumentException(
                        "Duplicate ProductVariant is not allowed.");
                }

                // 5. Validate ProductVariant and quantity
                foreach (var item in dto.Items)
                {
                    if (item.ExpectedQty <= 0)
                    {
                        throw new ArgumentException(
                            "Expected quantity must be greater than zero.");
                    }

                    if (item.ProductVariantId <= 0 ||
                        !await _repository.ProductVariantExistsAsync(
                            item.ProductVariantId))
                    {
                        throw new ArgumentException(
                            $"ProductVariant {item.ProductVariantId} does not exist.");
                    }
                }

                // 6. Create InboundRequest
                var request = new InboundRequest
                {
                    WarehouseId = dto.WarehouseId,
                    RequestedByUserId = requestedByUserId,
                    Status = "Pending",
                    CreatedDate = DateTime.UtcNow,
                    SupplierId = null,
                    ApprovedByUserId = null,
                    SupplierCommitmentStatus = null,
                    ExpectedDeliveryDate = null,
                    RejectionReason = null
                };

                // 7. Create InboundRequestItems
                var items = dto.Items.Select(item =>
                    new InboundRequestItem
                    {
                        ProductVariantId = item.ProductVariantId,
                        ExpectedQty = item.ExpectedQty
                    }).ToList();

                // 8. Save request and items
                var createdRequest =
                    await _repository.CreateAsync(request, items);

                return createdRequest.Id;
            }

            // Task 2: Get Inbound Request List

            // Task 2: Get Inbound Request List
            public async Task<List<InboundRequestListDto>> GetAllAsync()
            {
                var requests = await _repository.GetAllAsync();

                return requests.Select(request => new InboundRequestListDto
                {
                    Id = request.Id,
                    WarehouseId = request.WarehouseId,
                    RequestedByUserId = request.RequestedByUserId,
                    SupplierId = request.SupplierId,
                    Status = request.Status,
                    CreatedDate = request.CreatedDate,
                    ExpectedDeliveryDate = request.ExpectedDeliveryDate
                }).ToList();
            }

            // Task 3: Get Inbound Request Detail
            public async Task<InboundRequestDetailDto?> GetByIdAsync(int id)
            {
                if (id <= 0)
                {
                    return null;
                }

                // Get inbound request information
                var request = await _repository.GetByIdAsync(id);

                if (request == null)
                {
                    return null;
                }

                // Get products belonging to the request
                var items = await _repository.GetItemsByRequestIdAsync(id);

                // Map Entity to DTO
                return new InboundRequestDetailDto
                {
                    Id = request.Id,
                    WarehouseId = request.WarehouseId,
                    SupplierId = request.SupplierId,
                    RequestedByUserId = request.RequestedByUserId,
                    ApprovedByUserId = request.ApprovedByUserId,
                    SupplierCommitmentStatus = request.SupplierCommitmentStatus,
                    ExpectedDeliveryDate = request.ExpectedDeliveryDate,
                    RejectionReason = request.RejectionReason,
                    Status = request.Status,
                    CreatedDate = request.CreatedDate,

                    Items = items.Select(item => new InboundRequestDetailItemDto
                    {
                        Id = item.Id,
                        ProductVariantId = item.ProductVariantId,
                        ExpectedQty = item.ExpectedQty
                    }).ToList()
                };
            }

            public async Task UpdateSupplierCommitmentAsync(
                int requestId,
                UpdateSupplierCommitmentDto dto)
            {
                if (requestId <= 0)
                {
                    throw new ArgumentException("Invalid inbound request ID.");
                }

                var request = await _repository.GetByIdAsync(requestId);

                if (request == null)
                {
                    throw new KeyNotFoundException(
                        $"Inbound request #{requestId} was not found.");
                }

                if (request.Status != "Pending")
                {
                    throw new InvalidOperationException(
                        "Only pending inbound requests can be updated.");
                }

                if (!dto.SupplierId.HasValue || dto.SupplierId.Value <= 0)
                {
                    throw new ArgumentException("Valid Supplier ID is required.");
                }

            if (!await _repository.SupplierExistsAsync(dto.SupplierId.Value))
            {
                throw new ArgumentException(
                    $"Supplier #{dto.SupplierId.Value} does not exist.");
            }

            var commitmentStatus = dto.CommitmentStatus?.Trim();

                if (commitmentStatus != "Confirmed" &&
                    commitmentStatus != "Rejected")
                {
                    throw new ArgumentException(
                        "Commitment status must be Confirmed or Rejected.");
                }

                if (commitmentStatus == "Confirmed")
                {
                    if (!dto.ExpectedDeliveryDate.HasValue)
                    {
                        throw new ArgumentException(
                            "Expected delivery date is required when confirmed.");
                    }

                    if (dto.ExpectedDeliveryDate.Value.Date < DateTime.Today)
                    {
                        throw new ArgumentException(
                            "Expected delivery date cannot be in the past.");
                    }
                }

                request.SupplierId = dto.SupplierId.Value;
                request.SupplierCommitmentStatus = commitmentStatus;
                request.ExpectedDeliveryDate =
                    commitmentStatus == "Confirmed"
                        ? dto.ExpectedDeliveryDate
                        : null;

                await _repository.UpdateAsync(request);
            }

        // Task 5: Manager Approve / Reject Inbound Request
        public async Task ReviewInboundRequestAsync(
            int requestId,
            ReviewInboundRequestDto dto,
            int managerUserId)
        {
            // 1. Validate request ID
            if (requestId <= 0)
            {
                throw new ArgumentException(
                    "Invalid inbound request ID.");
            }

            // 2. Validate Manager User ID
            if (managerUserId <= 0 ||
                !await _repository.UserExistsAsync(managerUserId))
            {
                throw new ArgumentException(
                    "Manager user does not exist.");
            }

            // 3. Find inbound request
            var request = await _repository.GetByIdAsync(requestId);

            if (request == null)
            {
                throw new KeyNotFoundException(
                    $"Inbound request #{requestId} was not found.");
            }

            // 4. Only Pending requests can be reviewed
            if (request.Status != "Pending")
            {
                throw new InvalidOperationException(
                    "Only pending inbound requests can be reviewed.");
            }

            // 5. Validate decision
            var decision = dto.Decision?.Trim();

            if (decision != "Approved" && decision != "Rejected")
            {
                throw new ArgumentException(
                    "Decision must be Approved or Rejected.");
            }

            // 6. Validate rejection reason
            if (decision == "Rejected" &&
                string.IsNullOrWhiteSpace(dto.RejectionReason))
            {
                throw new ArgumentException(
                    "Rejection reason is required.");
            }

            // 7. Update request
            request.Status = decision;
            request.ApprovedByUserId = managerUserId;

            request.RejectionReason =
                decision == "Rejected"
                    ? dto.RejectionReason!.Trim()
                    : null;

            // 8. Save changes
            await _repository.UpdateAsync(request);
        }

        // Task 6: chỉ tính phiếu Goods Receipt đã được Manager duyệt.
        public async Task<InboundReceivingProgressDto?> GetReceivingProgressAsync(int inboundRequestId)
        {
            if (inboundRequestId <= 0)
            {
                throw new ArgumentException("Invalid inbound request ID.");
            }
            var request = await _repository.GetByIdAsync(inboundRequestId);
            if (request == null)
            {
                return null;
            }
            var requestItems = await _repository.GetItemsByRequestIdAsync(inboundRequestId);
            var receipts = await _repository.GetGoodsReceiptsByRequestIdAsync(inboundRequestId);
            var approvedReceiptIds = receipts.Where(x => x.Status == "Approved")
                .Select(x => x.Id).ToList();
            var receivedItems = await _repository.GetGoodsReceiptItemsByReceiptIdsAsync(approvedReceiptIds);
            var variantIds = requestItems.Select(x => x.ProductVariantId).Distinct().ToList();
            if (requestItems.Count == 0 || requestItems.Any(x => x.ExpectedQty <= 0)
                || receivedItems.Any(x => !variantIds.Contains(x.ProductVariantId)))
            {
                throw new InvalidOperationException("Inbound receiving quantities are inconsistent.");
            }
            var result = new InboundReceivingProgressDto
            {
                InboundRequestId = inboundRequestId,
                TotalExpectedQty = 0,
                TotalReceivedQty = 0,
                RemainingQty = 0
            };
            foreach (int variantId in variantIds)
            {
                int expected = requestItems.Where(x => x.ProductVariantId == variantId).Sum(x => x.ExpectedQty);
                int received = receivedItems.Where(x => x.ProductVariantId == variantId).Sum(x => x.ReceivedQty);
                if (received < 0 || received > expected)
                {
                    throw new InvalidOperationException("Received quantity exceeds expected quantity for a variant.");
                }
                result.Items.Add(new InboundReceivingProgressItemDto
                {
                    ProductVariantId = variantId,
                    ExpectedQty = expected,
                    ReceivedQty = received,
                    RemainingQty = expected - received
                });
                result.TotalExpectedQty += expected;
                result.TotalReceivedQty += received;
                result.RemainingQty += expected - received;
            }
            result.ProgressPercentage = Math.Round(100.0 * result.TotalReceivedQty.Value / result.TotalExpectedQty, 2);
            if (result.Items.All(x => x.RemainingQty == 0))
            {
                result.ProgressStatus = "Completed";
            }
            else if (result.TotalReceivedQty > 0)
            {
                result.ProgressStatus = "PartiallyReceived";
            }
            else
            {
                result.ProgressStatus = "Pending";
            }
            return result;
        }
    }
}
