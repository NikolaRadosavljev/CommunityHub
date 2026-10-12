using System.Collections.Generic;
using System.Linq;
using CommunityHub.Application.Services;
using CommunityHub.Application.Services.DTO;
using CommunityHub.Application.Domain;
using CommunityHub.Application.Database.Repositories;

namespace CommunityHub.Ui.ViewModels;

public class RentalRequestsViewModel
{
    private readonly long _tenantId;
    private readonly int _buildingId;
    private readonly CommonRoomService _commonRoomService;
    private readonly CommonSpaceRentalRequestService _rentalRequestService;
    private readonly UserDbRepository _userRepository;
    private readonly CommonSpaceDbRepository _commonSpaceRepository;

    public List<CommonRoomDTO> AvailableRooms { get; private set; }
    public List<RentalRequestDTO> MyRequests { get; private set; }

    public RentalRequestsViewModel(long tenantId, int buildingId)
    {
        _tenantId = tenantId;
        _buildingId = buildingId;
        _commonRoomService = new CommonRoomService();
        _rentalRequestService = new CommonSpaceRentalRequestService();
        _userRepository = new UserDbRepository();
        _commonSpaceRepository = new CommonSpaceDbRepository();
        AvailableRooms = new List<CommonRoomDTO>();
        MyRequests = new List<RentalRequestDTO>();
    }

    public void LoadCommonRooms()
    {
        try
        {
            AvailableRooms = _commonRoomService.GetCommonRoomsForBuilding(_buildingId);
        }
        catch (Exception ex)
        {
            throw new Exception($"Error loading common rooms: {ex.Message}", ex);
        }
    }

    public void LoadMyRequests()
    {
        try
        {
            // Učitaj sve zahteve za tu zgradu i filtrira za trenutnog tenant-a
            var buildingRepository = new BuildingDbRepository();
            var building = buildingRepository.GetById(_buildingId);
            if (building == null)
            {
                MyRequests = new List<RentalRequestDTO>();
                return;
            }

            var allRequests = _rentalRequestService.GetAllRequests(new List<int> { _buildingId });
            var tenantRequests = allRequests.Where(r => r.TenantId == _tenantId).ToList();

            // Učitaj sve CommonRooms za zgradu za mapping Floor informacije
            var commonRooms = _commonRoomService.GetCommonRoomsForBuilding(_buildingId);

            MyRequests = tenantRequests.Select(req => MapToDTO(req, commonRooms)).ToList();
        }
        catch (Exception ex)
        {
            throw new Exception($"Error loading requests: {ex.Message}", ex);
        }
    }

    public int CreateRentalRequest(int commonRoomId, DateTime startDate, DateTime endDate)
    {
        try
        {
            var createdRequest = _rentalRequestService.CreateRequest(commonRoomId, (int)_tenantId, startDate, endDate);
            return createdRequest.Id;
        }
        catch (Exception ex)
        {
            throw new Exception($"Error creating rental request: {ex.Message}", ex);
        }
    }

    public void CancelRequest(int requestId)
    {
        try
        {
            var request = _rentalRequestService.GetRequestById(requestId);
            if (request != null && (request.Status == RentalRequestStatus.Pending || request.Status == RentalRequestStatus.PendingChangeOfDate))
            {
                _rentalRequestService.RejectRequest(request);
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"Error cancelling request: {ex.Message}", ex);
        }
    }

    public void AcceptSuggestedDates(int requestId)
    {
        try
        {
            _rentalRequestService.AcceptDateChange(requestId);
        }
        catch (Exception ex)
        {
            throw new Exception($"Error accepting suggested dates: {ex.Message}", ex);
        }
    }

    private RentalRequestDTO MapToDTO(CommonSpaceRentalRequest request, List<CommonRoomDTO> commonRooms = null)
    {
        var commonSpace = _commonSpaceRepository.GetById(request.CommonSpaceId);
        var commonRoom = commonRooms?.FirstOrDefault(r => r.Id == request.CommonSpaceId);

        // Ako je status PendingChangeOfDate, to znači da su RequestedStartDate i RequestedEndDate
        // zapravo novi predloženi termini od strane upravnika
        DateTime? suggestedStart = null;
        DateTime? suggestedEnd = null;
        if (request.Status == RentalRequestStatus.PendingChangeOfDate)
        {
            suggestedStart = request.RequestedStartDate;
            suggestedEnd = request.RequestedEndDate;
        }

        return new RentalRequestDTO
        {
            Id = request.Id,
            CommonRoomId = request.CommonSpaceId,
            CommonRoomName = commonSpace?.Name ?? $"Prostorija #{request.CommonSpaceId}",
            TenantId = request.TenantId,
            StartDate = request.RequestedStartDate,
            EndDate = request.RequestedEndDate,
            Status = request.Status.ToString(),
            CreatedAt = DateTime.Now,
            ApprovedDate = request.ApprovedDate,
            RentalType = commonSpace?.RentalType.ToString() ?? "SingleDay",
            SuggestedStartDate = suggestedStart,
            SuggestedEndDate = suggestedEnd,
            Floor = commonRoom?.Floor
        };
    }
}