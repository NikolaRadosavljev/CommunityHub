using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using CommunityHub.Application.Domain;
using CommunityHub.Application.Services;
using CommunityHub.Application;
using CommunityHub.Ui.Views.CoordinatorViews;

namespace CommunityHub.Ui.Views
{
    public partial class NeighborhoodMembershipRequestsWindow : Window
    {
        private readonly long _userId;
        private readonly NeighborhoodMembershipRequestService _service;
        private List<NeighborhoodMembershipRequest> _requests;
        private List<NeighborhoodMembership> _memberships;

        public NeighborhoodMembershipRequestsWindow(long userId)
        {
            InitializeComponent();
            _userId = userId;
            _service = Injector.CreateInstance<NeighborhoodMembershipRequestService>();
            _requests = new List<NeighborhoodMembershipRequest>();
            _memberships = new List<NeighborhoodMembership>();

            LoadStatusFilter();
            LoadRequests();
            LoadMemberships();
        }

        private void LoadStatusFilter()
        {
            StatusFilterComboBox.Items.Add("All");
            StatusFilterComboBox.Items.Add("Pending");
            StatusFilterComboBox.Items.Add("Approved");
            StatusFilterComboBox.Items.Add("Rejected");
            StatusFilterComboBox.SelectedIndex = 0;
        }

        //private void LoadRequests()
        //{
        //    try
        //    {
        //        _requests = _service.GetRequestsByCoordinator(_userId);

        //        MessageBox.Show($"Broj zahteva: {_requests.Count}");

        //        var requestView = _requests.Select(r => new
        //        {
        //            Id = r.Id,
        //            CitizenFullName = r.Citizen != null
        //                ? $"{r.Citizen.User.Name} {r.Citizen.User.Surname}"
        //                : "",
        //            CitizenAddress = r.Citizen?.Address ?? "",
        //            NeighborhoodName = r.Neighborhood?.Name ?? "",
        //            CreatedAt = r.CreatedAt,
        //            Status = r.Status.ToString(),
        //            RejectionReason = r.RejectionReason
        //        }).ToList();

        //        RequestsDataGrid.ItemsSource = requestView;
        //    }
        //    catch (Exception ex)
        //    {
        //        MessageBox.Show(ex.ToString(), "Greška u LoadRequests",
        //            MessageBoxButton.OK, MessageBoxImage.Error);
        //    }
        //}

        private void LoadRequests()
        {
            _requests = _service.GetRequestsByCoordinator(_userId);

            var requestView = _requests.Select(r => new
            {
                Id = r.Id,
                CitizenFullName = r.Citizen != null
                    ? $"{r.Citizen.User.Name} {r.Citizen.User.Surname}"
                    : "",
                CitizenAddress = r.Citizen?.Address ?? "",
                NeighborhoodName = r.Neighborhood?.Name ?? "",
                CreatedAt = r.CreatedAt,
                Status = r.Status.ToString(),
                RejectionReason = r.RejectionReason
            }).ToList();

            RequestsDataGrid.ItemsSource = requestView;
        }

        private void LoadMemberships()
        {
            _memberships = _service.GetMembershipsByCoordinator(_userId);

            var membershipView = _memberships.Select(m => new
            {
                Id = m.Id,
                CitizenFullName = m.Citizen != null
                    ? $"{m.Citizen.User.Name} {m.Citizen.User.Surname}"
                    : "",
                CitizenAddress = m.Citizen?.Address ?? "",
                JoinedAt = m.JoinedAt
            }).ToList();

            MembershipsDataGrid.ItemsSource = membershipView;
        }

        //private void LoadMemberships()
        //{
        //    try
        //    {
        //        _memberships = _service.GetMembershipsByCoordinator(_userId);

        //        MessageBox.Show($"Broj članstava: {_memberships.Count}");

        //        var membershipView = _memberships.Select(m => new
        //        {
        //            Id = m.Id,
        //            CitizenFullName = m.Citizen != null
        //                ? $"{m.Citizen.User.Name} {m.Citizen.User.Surname}"
        //                : "",
        //            CitizenAddress = m.Citizen?.Address ?? "",
        //            JoinedAt = m.JoinedAt
        //        }).ToList();

        //        MembershipsDataGrid.ItemsSource = membershipView;
        //    }
        //    catch (Exception ex)
        //    {
        //        MessageBox.Show(ex.ToString(), "Greška u LoadMemberships",
        //            MessageBoxButton.OK, MessageBoxImage.Error);
        //    }
        //}

        private void FilterButton_Click(object sender, RoutedEventArgs e)
        {
            string selectedStatus = StatusFilterComboBox.SelectedItem?.ToString() ?? "All";

            IEnumerable<NeighborhoodMembershipRequest> filteredRequests = _requests;

            if (selectedStatus != "All")
            {
                filteredRequests = _requests.Where(r => r.Status.ToString() == selectedStatus);
            }

            var requestView = filteredRequests.Select(r => new
            {
                Id = r.Id,
                CitizenFullName = r.Citizen != null
                    ? $"{r.Citizen.User.Name} {r.Citizen.User.Surname}"
                    : "",
                CitizenAddress = r.Citizen?.Address ?? "",
                NeighborhoodName = r.Neighborhood?.Name ?? "",
                CreatedAt = r.CreatedAt,
                Status = r.Status.ToString(),
                RejectionReason = r.RejectionReason
            }).ToList();

            RequestsDataGrid.ItemsSource = requestView;
        }

        private void ApproveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (RequestsDataGrid.SelectedItem == null)
                {
                    MessageBox.Show("You must select a request.", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                long requestId = (long)RequestsDataGrid.SelectedItem
                    .GetType()
                    .GetProperty("Id")!
                    .GetValue(RequestsDataGrid.SelectedItem)!;

                NeighborhoodMembershipRequest? selectedRequest = _requests.FirstOrDefault(r => r.Id == requestId);

                if (selectedRequest == null)
                {
                    MessageBox.Show("Zahtev nije pronađen.", "Greška",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (selectedRequest.Status != NeighborhoodMembershipRequestStatus.Pending)
                {
                    MessageBox.Show("Možete odobriti samo zahtev koji je u statusu Pending.", "Greška",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                _service.ApproveRequest(requestId);

                MessageBox.Show("The request has been successfully approved.", "Success",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                LoadRequests();
                LoadMemberships();
                RejectionReasonTextBox.Clear();
                SelectedRequestTextBlock.Text = "Nijedan zahtev nije izabran.";
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RejectButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (RequestsDataGrid.SelectedItem == null)
                {
                    MessageBox.Show("You must select a request.", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                long requestId = (long)RequestsDataGrid.SelectedItem
                    .GetType()
                    .GetProperty("Id")!
                    .GetValue(RequestsDataGrid.SelectedItem)!;

                NeighborhoodMembershipRequest? selectedRequest = _requests.FirstOrDefault(r => r.Id == requestId);

                if (selectedRequest == null)
                {
                    MessageBox.Show("Zahtev nije pronađen.", "Greška",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (selectedRequest.Status != NeighborhoodMembershipRequestStatus.Pending)
                {
                    MessageBox.Show("Možete odbiti samo zahtev koji je u statusu Pending.", "Greška",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                string? rejectionReason = string.IsNullOrWhiteSpace(RejectionReasonTextBox.Text)
                    ? null
                    : RejectionReasonTextBox.Text.Trim();

                _service.RejectRequest(requestId, rejectionReason);

                MessageBox.Show("The request has been successfully rejected.", "Success",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                LoadRequests();
                LoadMemberships();
                RejectionReasonTextBox.Clear();
                SelectedRequestTextBlock.Text = "Nijedan zahtev nije izabran.";
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            HomeWindow homeWindow = new HomeWindow(_userId);
            homeWindow.Show();
            this.Close();
        }


        private void RequestsDataGrid_SelectionChanged(object sender, RoutedEventArgs e)
        {
            if (RequestsDataGrid.SelectedItem == null)
            {
                SelectedRequestTextBlock.Text = "Nijedan zahtev nije izabran.";
                RejectionReasonTextBox.Clear();
                return;
            }

            long requestId = (long)RequestsDataGrid.SelectedItem
                .GetType()
                .GetProperty("Id")!
                .GetValue(RequestsDataGrid.SelectedItem)!;

            NeighborhoodMembershipRequest? selectedRequest = _requests.FirstOrDefault(r => r.Id == requestId);

            if (selectedRequest == null)
            {
                SelectedRequestTextBlock.Text = "Nijedan zahtev nije izabran.";
                RejectionReasonTextBox.Clear();
                return;
            }

            string citizenName = selectedRequest.Citizen != null
                ? $"{selectedRequest.Citizen.User.Name} {selectedRequest.Citizen.User.Surname}"
                : "Nepoznat građanin";

            string neighborhoodName = selectedRequest.Neighborhood?.Name ?? "Nepoznat kvart";

            SelectedRequestTextBlock.Text =
                $"Izabran zahtev: {citizenName} → {neighborhoodName} | Status: {selectedRequest.Status}";

            RejectionReasonTextBox.Text = selectedRequest.RejectionReason ?? "";
        }
    }
}