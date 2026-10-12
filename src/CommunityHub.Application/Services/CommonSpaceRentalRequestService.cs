using CommunityHub.Application.Database.Repositories;
using CommunityHub.Application.Domain;

namespace CommunityHub.Application.Services;

public class CommonSpaceRentalRequestService : ICommonSpaceRentalRequestService
{
    private readonly CommonSpaceRentalRequestDbRepository _repository;
    private readonly CommonSpaceDbRepository _commonSpaceRepository;

    public CommonSpaceRentalRequestService()
    {
        _repository = new CommonSpaceRentalRequestDbRepository();
        _commonSpaceRepository = new CommonSpaceDbRepository();
    }

    // Kreira novi zahtev i odmah primenjuje automatsku logiku odobravanja/odbijanja
    public CommonSpaceRentalRequest CreateRequest(int commonSpaceId, int tenantId, DateTime startDate, DateTime endDate)
    {
        // Privremeni objekat bez ID-a za kreiranje
        var request = new CommonSpaceRentalRequest(0, commonSpaceId, tenantId, startDate, endDate, RentalRequestStatus.Pending);

        // Odmah pokrenuti automatsku logiku pre nego što se sačuva u bazu
        ProcessNewRequest(request);

        // Sačuvati u bazu (Insert metoda ne menja Id, ali status/approvedDate su ažurirani)
        var inserted = _repository.Insert(request);

        return inserted;
    }

    // Automatska logika koja se poziva i kada stanar prihvati promenu termina
    // (poziva se iz UpdateStatus nakon što se status promeni iz PendingChangeOfDate u Pending)
    public void ProcessNewRequest(CommonSpaceRentalRequest request)
    {
        var rentals = _commonSpaceRepository.GetApprovedRequestsByCommonSpaceId(request.CommonSpaceId);

        // Moramo znati rental type prostorije da bismo znali logiku
        // Koristimo GetByBuildingId ili direktno gledamo po common_space_id
        // Pošto ne postoji GetById za CommonSpace, koristimo pomoćnu metodu
        var commonSpace = GetCommonSpaceById(request.CommonSpaceId);
        if (commonSpace == null) return;

        if (commonSpace.RentalType == CommonSpaceRentalType.SingleDay)
        {
            ProcessSingleDayRequest(request, rentals);
        }
        else
        {
            ProcessMultipleDaysRequest(request, rentals);
        }
    }

    private void ProcessSingleDayRequest(CommonSpaceRentalRequest request, List<CommonSpaceRentalRequest> existingApproved)
    {
        var freeDays = GetFreeDays(request, existingApproved);
        if (freeDays.Count == 0)
        {
            // Nema slobodnih dana - automatski odbiti
            request.ChangeStatus(RentalRequestStatus.Rejected);
        }
        // Ako ima slobodnih dana - ostaje Pending, upravnik bira
    }

    private void ProcessMultipleDaysRequest(CommonSpaceRentalRequest request, List<CommonSpaceRentalRequest> existingApproved)
    {
        var occupiedDates = GetOccupiedDatesSet(existingApproved);
        int numberOfDays = (int)(request.RequestedEndDate.Date - request.RequestedStartDate.Date).TotalDays + 1;

        bool isRangeAvailable = IsRangeAvailable(request.RequestedStartDate, request.RequestedEndDate, occupiedDates);

        if (isRangeAvailable)
        {
            // Ceo opseg slobodan - automatski odobriti
            request.ChangeStatus(RentalRequestStatus.Approved);
        }
        // Ako nije slobodan - ostaje Pending, upravnik bira alternativni opseg
    }

    private static bool IsRangeAvailable(DateTime start, DateTime end, HashSet<DateTime> occupiedDates)
    {
        for (var d = start.Date; d <= end.Date; d = d.AddDays(1))
        {
            if (occupiedDates.Contains(d)) return false;
        }
        return true;
    }

    private static HashSet<DateTime> GetOccupiedDatesSet(List<CommonSpaceRentalRequest> approvedRequests)
    {
        var occupied = new HashSet<DateTime>();
        foreach (var r in approvedRequests)
        {
            foreach (var d in r.GetOccupiedDates())
                occupied.Add(d.Date);
        }
        return occupied;
    }

    // Helper - nedostaje GetById za CommonSpace u repozitorijumu, privremeno Resolvedje
    private CommonSpace? GetCommonSpaceById(int commonSpaceId)
    {
        // Koristimo malo hacky pristup - pozivamo po building_id nije opcija ovde
        // pa dodajemo GetById u CommonSpaceDbRepository
        return _commonSpaceRepository.GetById(commonSpaceId);
    }

    public List<CommonSpaceRentalRequest> GetPendingRequests(List<int> buildingIds)
    {
        return _repository.GetPendingRequestsForManagerBuildings(buildingIds);
    }

    public List<CommonSpaceRentalRequest> GetAllRequests(List<int> buildingIds)
    {
        return _repository.GetAllRequestsForManagerBuildings(buildingIds);
    }

    public List<CommonSpaceRentalRequest> GetAllRequests()
    {
        return _repository.GetAllRequests();
    }

    public CommonSpaceRentalRequest? GetRequestById(int id)
    {
        return _repository.GetById(id);
    }

    // Za SingleDay: vraća slobodne dane u opsegu koji je stanar zadao
    public List<DateTime> GetFreeDaysForRequest(int requestId)
    {
        var request = _repository.GetById(requestId);
        if (request == null) return new List<DateTime>();

        var rentals = _commonSpaceRepository.GetApprovedRequestsByCommonSpaceId(request.CommonSpaceId);
        return GetFreeDays(request, rentals);
    }

    private static List<DateTime> GetFreeDays(CommonSpaceRentalRequest request, List<CommonSpaceRentalRequest> approvedRentals)
    {
        var occupiedDates = GetOccupiedDatesSet(approvedRentals);
        var freeDates = new List<DateTime>();

        for (var d = request.RequestedStartDate.Date; d <= request.RequestedEndDate.Date; d = d.AddDays(1))
        {
            if (!occupiedDates.Contains(d))
                freeDates.Add(d);
        }
        return freeDates;
    }

    // Za MultipleDays: vraća slobodne opsege koji imaju isti broj dana kao originalni zahtev
    // Pretražuje 90 dana unapred od danas kako bi pronašao alternative
    public List<(DateTime Start, DateTime End)> GetFreeRangesForRequest(int requestId)
    {
        var request = _repository.GetById(requestId);
        if (request == null) return new List<(DateTime, DateTime)>();

        var rentals = _commonSpaceRepository.GetApprovedRequestsByCommonSpaceId(request.CommonSpaceId);
        var occupiedDates = GetOccupiedDatesSet(rentals);

        int numberOfDays = (int)(request.RequestedEndDate.Date - request.RequestedStartDate.Date).TotalDays + 1;

        var freeRanges = new List<(DateTime Start, DateTime End)>();

        // Pretraga od danas, 90 dana unapred
        DateTime searchStart = DateTime.Today;
        DateTime searchEnd = DateTime.Today.AddDays(90);

        for (var d = searchStart; d <= searchEnd.AddDays(-(numberOfDays - 1)); d = d.AddDays(1))
        {
            var rangeEnd = d.AddDays(numberOfDays - 1);

            // Preskočiti originalni opseg stanara (koji nije slobodan)
            if (d == request.RequestedStartDate.Date) continue;

            if (IsRangeAvailable(d, rangeEnd, occupiedDates))
            {
                freeRanges.Add((d, rangeEnd));
            }
        }

        return freeRanges;
    }

    public void SelectDayForSingleDayRental(CommonSpaceRentalRequest request, DateTime selectedDate)
    {
        request.SetApprovedDate(selectedDate);
        _repository.UpdateStatus(request);
    }

    public void SuggestDatesForMultipleDaysRental(CommonSpaceRentalRequest request, DateTime newStart, DateTime newEnd)
    {
        request.UpdateRequestedDates(newStart, newEnd);
        _repository.UpdateStatus(request);
    }

    public void RejectRequest(CommonSpaceRentalRequest request)
    {
        request.ChangeStatus(RentalRequestStatus.Rejected);
        _repository.UpdateStatus(request);
    }

    public void ApproveRequest(CommonSpaceRentalRequest request)
    {
        request.ChangeStatus(RentalRequestStatus.Approved);
        _repository.UpdateStatus(request);
    }

    // Poziva se kada stanar prihvati promenu termina (status: PendingChangeOfDate -> Pending)
    public void AcceptDateChange(int requestId)
    {
        var request = _repository.GetById(requestId);
        if (request == null) return;

        // Stanar prihvata predloženi termin - status ide na Pending
        request.ChangeStatus(RentalRequestStatus.Pending);

        // Sačuvati novi status u bazu
        _repository.UpdateStatus(request);
        
        // VAŽNO: Pozovi automatsku logiku obrade nakon potvrde termina
        // Sistem će proveriti da li je termin sada slobodan i automatski odobriti ako jeste
        // Ili vratiti upravniku sa novom porukom ako nije slobodan
        ProcessNewRequest(request);
        
        // Ažuriri status u bazi sa rezultatom automatske logike
        _repository.UpdateStatus(request);
    }

    // Stara metoda - zadržana radi kompatibilnosti
    public List<DateTime> GetFreeDatesForSingleDay(CommonSpaceRentalRequest request)
    {
        var rentals = _commonSpaceRepository.GetApprovedRequestsByCommonSpaceId(request.CommonSpaceId);
        return GetFreeDays(request, rentals);
    }
}
