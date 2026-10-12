using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using CommunityHub.Application.Database.Repositories;
using CommunityHub.Application.Domain;
using CommunityHub.Application.Services;

namespace CommunityHub.Ui.Views;

public partial class CommonRoomCalendarPage : Page
{
    private readonly CommonSpace _commonSpace;
    private readonly CommonSpaceService _commonSpaceService;

    public CommonRoomCalendarPage(CommonSpace space)
    {
        InitializeComponent();
        _commonSpace = space;
        _commonSpaceService = new CommonSpaceService(new CommonSpaceDbRepository(), new BuildingDbRepository());

        TxtRoomName.Text = _commonSpace.Name;
        TxtRentalPeriod.Text = _commonSpace.GetRentalTypeLabel();

        LoadCalendar();
    }

    private void LoadCalendar()
    {
        var approvedRequests = _commonSpaceService.GetApprovedRequestsForCommonSpace(_commonSpace.Id);

        var occupiedDates = approvedRequests
            .SelectMany(r => r.GetOccupiedDates())
            .ToHashSet();

        MarkOccupied(occupiedDates);
    }

    private void MarkOccupied(HashSet<DateTime> occupiedDates)
    {
        RoomCalendar.BlackoutDates.Clear();

        foreach (var date in occupiedDates)
        {
            RoomCalendar.BlackoutDates.Add(new CalendarDateRange(date));
        }
    }
}