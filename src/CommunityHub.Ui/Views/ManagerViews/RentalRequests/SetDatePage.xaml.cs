using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using CommunityHub.Application.Services;
using CommunityHub.Ui.Views;

namespace CommunityHub.Ui.Views.ManagerViews.RentalRequests;

public partial class SetDatePage : Page
{
    private readonly int _requestId;
    private readonly CommonSpaceRentalRequestService _rentalService;
    private List<(DateTime Start, DateTime End)> _freeRanges = new();

    public SetDatePage(int requestId)
    {
        InitializeComponent();
        _requestId = requestId;
        _rentalService = new CommonSpaceRentalRequestService();
        Loaded += (s, e) => LoadData();
    }

    private void LoadData()
    {
        var mainWin = ManagerMainWindow.GetFrom(this);
        var request = _rentalService.GetRequestById(_requestId);
        if (request == null)
        {
            mainWin?.ShowErrorToast("Request not found.");
            if (NavigationService.CanGoBack) NavigationService.GoBack();
            return;
        }

        // Prikazati opseg koji je stanar zadao
        TxtRequestedRange.Text = $"{request.RequestedStartDate:dd.MM.yyyy} - {request.RequestedEndDate:dd.MM.yyyy}";

        int numberOfDays = (int)(request.RequestedEndDate.Date - request.RequestedStartDate.Date).TotalDays + 1;
        TxtNumberOfDays.Text = $"Requested duration: {numberOfDays} day(s)";

        // Dohvatiti slobodne opsege koji imaju isti broj dana
        _freeRanges = _rentalService.GetFreeRangesForRequest(_requestId);

        if (_freeRanges.Count == 0)
        {
            mainWin?.ShowErrorToast("There are no available date ranges with the requested duration. Please contact the tenant.");
            if (NavigationService.CanGoBack) NavigationService.GoBack();
            return;
        }

        DatesList.ItemsSource = _freeRanges.Select(r => new DateOptionViewModel
        {
            DateString = $"{r.Start:dd.MM.yyyy} - {r.End:dd.MM.yyyy}"
        }).ToList();
    }

    private void SendButton_Click(object sender, RoutedEventArgs e)
    {
        var mainWin = ManagerMainWindow.GetFrom(this);

        // Pronaći odabrani radio button
        int selectedIndex = -1;
        for (int i = 0; i < DatesList.Items.Count; i++)
        {
            var container = DatesList.ItemContainerGenerator.ContainerFromIndex(i) as ListBoxItem;
            if (container != null)
            {
                var radio = FindRadioButton(container);
                if (radio != null && radio.IsChecked == true)
                {
                    selectedIndex = i;
                    break;
                }
            }
        }

        if (selectedIndex < 0 || selectedIndex >= _freeRanges.Count)
        {
            mainWin?.ShowErrorToast("Please select a date range.");
            return;
        }

        var request = _rentalService.GetRequestById(_requestId);
        if (request == null) return;

        var (newStart, newEnd) = _freeRanges[selectedIndex];
        _rentalService.SuggestDatesForMultipleDaysRental(request, newStart, newEnd);

        mainWin?.ShowSuccessToast($"New date range suggested: {newStart:dd.MM.yyyy} - {newEnd:dd.MM.yyyy}.", goBack: true);
    }

    private static RadioButton? FindRadioButton(DependencyObject parent)
    {
        for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
            if (child is RadioButton rb) return rb;
            var found = FindRadioButton(child);
            if (found != null) return found;
        }
        return null;
    }
}
