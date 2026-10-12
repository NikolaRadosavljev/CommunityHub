using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using CommunityHub.Application;
using CommunityHub.Application.Domain;
using CommunityHub.Application.Services;
using CommunityHub.Ui.Commands;

namespace CommunityHub.Ui.ViewModels.CitizenViewModels
{
    public class RegisterVolunteerViewModel : ViewModelBase
    {
        private readonly long _userId;
        private readonly Event _event;
        private readonly Window _window;
        private readonly EventService _eventService;

        public ObservableCollection<EventItemItemViewModel> Items { get; }
        public ICommand RegisterCommand { get; }

        public RegisterVolunteerViewModel(long userId, Event ev, Window window)
        {
            _userId = userId;
            _event = ev;
            _window = window;
            _eventService = Injector.CreateInstance<EventService>();

            var viewModels = ev.Items?
                .Select(i => new EventItemItemViewModel(i))
                .ToList() ?? new List<EventItemItemViewModel>();

            Items = new ObservableCollection<EventItemItemViewModel>(viewModels);

            RegisterCommand = new RelayCommand(ExecuteRegister);
        }

        private void ExecuteRegister(object parameter)
        {
            List<long> selectedItemIds = Items
                .Where(vm => vm.IsChecked)
                .Select(vm => vm.Item.Id)
                .ToList();

            try
            {
                string result = _eventService.RegisterVolunteer(_event, _userId, selectedItemIds);
                MessageBox.Show(result, "Notice", MessageBoxButton.OK, MessageBoxImage.Information);

                if (_window != null)
                {
                    _window.DialogResult = true;
                    _window.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred during registration: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    public class EventItemItemViewModel : ViewModelBase
    {
        private bool _isChecked;

        public EventItem Item { get; }
        public bool IsEnabled => !Item.IsCovered;
        public string StatusText => Item.IsCovered ? "Someone is bringing it" : "No one has registered for this item";

        public bool IsChecked
        {
            get => _isChecked;
            set
            {
                _isChecked = value;
                OnPropertyChanged();
            }
        }

        public EventItemItemViewModel(EventItem item)
        {
            Item = item;
            _isChecked = false;
        }
    }
}