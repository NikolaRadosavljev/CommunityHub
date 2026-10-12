using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using CommunityHub.Application.Services;
using CommunityHub.Ui.Views;

namespace CommunityHub.Ui.Views.ManagerViews.RentalRequests;

public partial class SelectDayPage : Page
{
    private readonly int _requestId;
    private readonly CommonSpaceRentalRequestService _rentalService;
    private List<DateTime> _freeDays = new();

    public SelectDayPage(int requestId)
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

        // Dohvatiti slobodne dane
        _freeDays = _rentalService.GetFreeDaysForRequest(_requestId);

        if (_freeDays.Count == 0)
        {
            // Nema slobodnih dana - odmah odbiti i obavijestiti
            var req = _rentalService.GetRequestById(_requestId);
            if (req != null)
            {
                _rentalService.RejectRequest(req);
            }
            mainWin?.ShowErrorToast("There are no free days in the requested range. The request has been automatically rejected.");
            if (NavigationService.CanGoBack) NavigationService.GoBack();
            return;
        }

        DaysList.ItemsSource = _freeDays.Select(d => new DateOptionViewModel
        {
            DateString = d.ToString("dd.MM.yyyy (dddd)")
        }).ToList();
    }

    private void SendButton_Click(object sender, RoutedEventArgs e)
    {
        // Pronaći odabrani radio button
        int selectedIndex = -1;
        for (int i = 0; i < DaysList.Items.Count; i++)
        {
            var container = DaysList.ItemContainerGenerator.ContainerFromIndex(i) as ListBoxItem;
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

        if (selectedIndex < 0 || selectedIndex >= _freeDays.Count)
        {
            ManagerMainWindow.GetFrom(this)?.ShowErrorToast("Please select a day.");
            return;
        }

        var request = _rentalService.GetRequestById(_requestId);
        if (request == null) return;

        DateTime selectedDay = _freeDays[selectedIndex];
        _rentalService.SelectDayForSingleDayRental(request, selectedDay);

        ManagerMainWindow.GetFrom(this)?.ShowSuccessToast($"Request approved for {selectedDay:dd.MM.yyyy}.", goBack: true);
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

public class DateOptionViewModel
{
    public string DateString { get; set; } = string.Empty;
}
