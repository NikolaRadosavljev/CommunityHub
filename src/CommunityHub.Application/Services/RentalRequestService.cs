using CommunityHub.Application.Database.Repositories;
using CommunityHub.Application.Domain;
using CommunityHub.Application.Services.DTO;

namespace CommunityHub.Application.Services;

public class RentalRequestService
{
    private readonly RentalRequestDbRepository _rentalRequestRepository;
    private readonly CommonRoomDbRepository _commonRoomRepository;

    public RentalRequestService()
    {
        _rentalRequestRepository = new RentalRequestDbRepository();
        _commonRoomRepository = new CommonRoomDbRepository();
    }

    public RentalRequestDTO CreateRentalRequest(CreateRentalRequestDTO request)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Validate common room exists
        CommonRoom? room = _commonRoomRepository.GetById(request.CommonRoomId);
        if (room == null)
        {
            throw new ArgumentException($"Common room with ID {request.CommonRoomId} does not exist.", nameof(request.CommonRoomId));
        }

        // Validate dates
        if (request.StartDate.Date < DateTime.Now.Date)
        {
            throw new ArgumentException("Start date cannot be in the past.", nameof(request.StartDate));
        }

        if (request.EndDate.Date < DateTime.Now.Date)
        {
            throw new ArgumentException("End date cannot be in the past.", nameof(request.EndDate));
        }

        if (request.StartDate > request.EndDate)
        {
            throw new ArgumentException("Start date must be before end date.", nameof(request.StartDate));
        }

        // For single-day rooms, start and end date must be the same
        if (room.RentalType == "SingleDay" && request.StartDate.Date != request.EndDate.Date)
        {
            throw new ArgumentException("This common room is only available for single-day rentals.", nameof(request.EndDate));
        }

        // Check for date conflicts with approved requests
        bool hasConflict = CheckDateConflict(request.CommonRoomId, request.StartDate, request.EndDate);
        string status = "Pending";
        DateTime? suggestedStartDate = null;
        DateTime? suggestedEndDate = null;

        if (hasConflict)
        {
            status = "Pending - Term Change";
            // Find next available dates
            (suggestedStartDate, suggestedEndDate) = FindNextAvailableDates(request.CommonRoomId, request.StartDate, request.EndDate, room.RentalType);
        }

        // Create rental request
        RentalRequest rentalRequest = new RentalRequest(
            id: 0,
            commonRoomId: request.CommonRoomId,
            tenantId: request.TenantId,
            startDate: request.StartDate,
            endDate: request.EndDate,
            status: status,
            createdAt: DateTime.Now,
            approvedDate: null,
            suggestedStartDate: suggestedStartDate,
            suggestedEndDate: suggestedEndDate
        );

        int createdId = _rentalRequestRepository.Create(rentalRequest);

        return GetRentalRequestById(createdId) ?? throw new InvalidOperationException("Failed to retrieve created rental request.");
    }

    private bool CheckDateConflict(int commonRoomId, DateTime startDate, DateTime endDate)
    {
        List<RentalRequest> requests = _rentalRequestRepository.GetAllByCommonRoomId(commonRoomId);

        foreach (var req in requests)
        {
            // Check if there's an overlap with an approved request
            if (req.Status == "Approved")
            {
                if (!(endDate.Date < req.StartDate.Date || startDate.Date > req.EndDate.Date))
                {
                    return true; // Dates overlap
                }
            }
        }

        return false;
    }

    private (DateTime, DateTime) FindNextAvailableDates(int commonRoomId, DateTime originalStart, DateTime originalEnd, string rentalType)
    {
        TimeSpan requestedDuration = originalEnd.Date - originalStart.Date;
        DateTime searchStart = originalStart.AddDays(1);
        var approvedRequests = _rentalRequestRepository.GetAllByCommonRoomId(commonRoomId)
            .Where(r => r.Status == "Approved")
            .OrderBy(r => r.StartDate)
            .ToList();

        // For single day, find first available day
        if (rentalType == "SingleDay")
        {
            DateTime currentDay = searchStart.Date;
            while (true)
            {
                bool isOccupied = approvedRequests.Any(r =>
                    r.StartDate.Date <= currentDay && currentDay <= r.EndDate.Date);

                if (!isOccupied)
                {
                    return (currentDay, currentDay);
                }

                currentDay = currentDay.AddDays(1);

                // Safety check to avoid infinite loop
                if (currentDay > DateTime.Now.AddMonths(3))
                    break;
            }
        }
        else
        {
            // For multiple days, find first available period of same duration
            DateTime currentStart = searchStart.Date;
            while (true)
            {
                DateTime currentEnd = currentStart.Add(requestedDuration);
                bool isAvailable = !approvedRequests.Any(r =>
                    !(currentEnd.Date < r.StartDate.Date || currentStart.Date > r.EndDate.Date));

                if (isAvailable)
                {
                    return (currentStart, currentEnd);
                }

                currentStart = currentStart.AddDays(1);

                // Safety check to avoid infinite loop
                if (currentStart > DateTime.Now.AddMonths(3))
                    break;
            }
        }

        // Fallback: suggest same dates but 7 days later
        return (originalStart.AddDays(7), originalEnd.AddDays(7));
    }


    public List<RentalRequestDTO> GetRentalRequestsForTenant(long tenantId)
    {
        if (tenantId == 0)
        {
            throw new ArgumentException("Tenant ID cannot be 0.", nameof(tenantId));
        }

        List<RentalRequest> requests = _rentalRequestRepository.GetAllByTenantId(tenantId);

        return requests.Select(req => MapToDTO(req)).ToList();
    }

    public List<RentalRequestDTO> GetRentalRequestsForTenantInBuilding(long tenantId, int buildingId)
    {
        if (tenantId == 0)
        {
            throw new ArgumentException("Tenant ID cannot be 0.", nameof(tenantId));
        }

        if (buildingId == 0)
        {
            throw new ArgumentException("Building ID cannot be 0.", nameof(buildingId));
        }

        List<RentalRequest> requests = _rentalRequestRepository.GetAllByTenantIdAndBuilding(tenantId, buildingId);

        return requests.Select(req => MapToDTO(req)).ToList();
    }

    public RentalRequestDTO? GetRentalRequestById(int requestId)
    {
        if (requestId == 0)
        {
            throw new ArgumentException("Request ID cannot be 0.", nameof(requestId));
        }

        RentalRequest? request = _rentalRequestRepository.GetById(requestId);

        if (request == null)
        {
            return null;
        }

        return MapToDTO(request);
    }

    public void UpdateRequestStatus(int requestId, string newStatus)
    {
        if (requestId == 0)
        {
            throw new ArgumentException("Request ID must be greater than 0.", nameof(requestId));
        }

        if (string.IsNullOrWhiteSpace(newStatus))
        {
            throw new ArgumentException("Status cannot be empty.", nameof(newStatus));
        }

        RentalRequest? request = _rentalRequestRepository.GetById(requestId);
        if (request == null)
        {
            throw new ArgumentException($"Rental request with ID {requestId} does not exist.", nameof(requestId));
        }

        _rentalRequestRepository.UpdateStatus(requestId, newStatus);
    }


    public void AcceptSuggestedDates(int requestId)
    {
        if (requestId == 0)
        {
            throw new ArgumentException("Request ID must be greater than 0.", nameof(requestId));
        }

        RentalRequest? request = _rentalRequestRepository.GetById(requestId);
        if (request == null)
        {
            throw new ArgumentException($"Rental request with ID {requestId} does not exist.", nameof(requestId));
        }

        if (request.Status != "Pending - Term Change")
        {
            throw new InvalidOperationException("This request does not have a term change pending.");
        }

        if (!request.SuggestedStartDate.HasValue || !request.SuggestedEndDate.HasValue)
        {
            throw new InvalidOperationException("No suggested dates available for this request.");
        }

        // Update dates and return status to "Pending"
        _rentalRequestRepository.AcceptSuggestedDates(requestId);
    }

    public void CancelRequest(int requestId)
    {
        if (requestId == 0)
        {
            throw new ArgumentException("Request ID must be greater than 0.", nameof(requestId));
        }

        RentalRequest? request = _rentalRequestRepository.GetById(requestId);
        if (request == null)
        {
            throw new ArgumentException($"Rental request with ID {requestId} does not exist.", nameof(requestId));
        }

        if (request.Status != "Pending" && request.Status != "PendingChangeOfDate")
        {
            throw new InvalidOperationException($"Cannot cancel a request with status '{request.Status}'.");
        }

        _rentalRequestRepository.CancelRequest(requestId);
    }

    private RentalRequestDTO MapToDTO(RentalRequest request)
    {
        CommonRoom? room = _commonRoomRepository.GetById(request.CommonRoomId);

        return new RentalRequestDTO
        {
            Id = request.Id,
            CommonRoomId = request.CommonRoomId,
            CommonRoomName = room?.Name ?? "Unknown Room",
            TenantId = request.TenantId,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Status = request.Status,
            CreatedAt = request.CreatedAt,
            ApprovedDate = request.ApprovedDate,
            RentalType = room?.RentalType ?? "SingleDay",
            SuggestedStartDate = request.SuggestedStartDate,
            SuggestedEndDate = request.SuggestedEndDate
        };
    }
}
