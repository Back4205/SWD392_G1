using EWMS.Api.DTOs.InboundRequests;

namespace EWMS.Api.Interfaces
{
    public interface IInboundRequestService
    {
        // Task 1: Create Inbound Request
        Task<int> CreateAsync(
            CreateInboundRequestDto dto,
            int requestedByUserId);

        // Task 2: Get Inbound Request List
        Task<List<InboundRequestListDto>> GetAllAsync();

        // Task 3: Get Inbound Request Detail
        Task<InboundRequestDetailDto?> GetByIdAsync(int id);

        Task UpdateSupplierCommitmentAsync(
            int requestId,
            UpdateSupplierCommitmentDto dto
        );

        // Task 5: Manager Approve / Reject Inbound Request
        Task ReviewInboundRequestAsync(
            int requestId,
            ReviewInboundRequestDto dto,
            int managerUserId
        );

        // Task 6: Get Receiving Progress
        Task<InboundReceivingProgressDto?> GetReceivingProgressAsync(
            int inboundRequestId
        );


    }
}
