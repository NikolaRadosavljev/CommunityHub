using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using CommunityHub.Application.Domain;
using CommunityHub.Application.Services;
using CommunityHub.Ui.Commands;
using CommunityHub.Application;

namespace CommunityHub.Ui.ViewModels.CitizenViewModels
{
    public class CreateEventViewModel : ViewModelBase
    {
        private readonly long _userId;
        private readonly long _neighborhoodId;
        private readonly Window _window;
        private readonly EventService _eventService;

        private string _name;
        private string _description;
        private DateTime? _date = DateTime.Today;
        private string _startTime = "12:00";
        private string _duration;
        private string _minVolunteers;
        private string _itemsRawText;
        private string _validationError;
        private bool _isValidationErrorVisible;

        public ICommand CreateEventCommand { get; }
        public ICommand CloseCommand { get; }

        public CreateEventViewModel(long userId, long neighborhoodId, Window window)
        {
            _userId = userId;
            _neighborhoodId = neighborhoodId;
            _window = window;
            _eventService = Injector.CreateInstance<EventService>();

            CreateEventCommand = new RelayCommand(ExecuteCreateEvent);
            CloseCommand = new RelayCommand(ExecuteClose);
        }

        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(); }
        }

        public string Description
        {
            get => _description;
            set { _description = value; OnPropertyChanged(); }
        }

        public DateTime? Date
        {
            get => _date;
            set { _date = value; OnPropertyChanged(); }
        }

        public string StartTime
        {
            get => _startTime;
            set { _startTime = value; OnPropertyChanged(); }
        }

        public string Duration
        {
            get => _duration;
            set { _duration = value; OnPropertyChanged(); }
        }

        public string MinVolunteers
        {
            get => _minVolunteers;
            set { _minVolunteers = value; OnPropertyChanged(); }
        }

        public string ItemsRawText
        {
            get => _itemsRawText;
            set { _itemsRawText = value; OnPropertyChanged(); }
        }

        public string ValidationError
        {
            get => _validationError;
            set
            {
                _validationError = value;
                OnPropertyChanged();
                IsValidationErrorVisible = !string.IsNullOrEmpty(value);
            }
        }

        public bool IsValidationErrorVisible
        {
            get => _isValidationErrorVisible;
            set { _isValidationErrorVisible = value; OnPropertyChanged(); }
        }

        private void ExecuteCreateEvent(object parameter)
        {
            ValidationError = string.Empty;

            if (string.IsNullOrWhiteSpace(Name))
            {
                ValidationError = "Naziv događaja je obavezan.";
                return;
            }

            if (!Date.HasValue)
            {
                ValidationError = "Molimo izaberite validan datum.";
                return;
            }

            if (!TimeSpan.TryParse(StartTime, out TimeSpan parsedTime))
            {
                ValidationError = "Vreme početka mora biti u formatu HH:mm (npr. 14:30).";
                return;
            }

            if (!int.TryParse(Duration, out int parsedDuration) || parsedDuration <= 0)
            {
                ValidationError = "Trajanje mora biti pozitivan broj minuta.";
                return;
            }

            if (!int.TryParse(MinVolunteers, out int parsedMinVolunteers) || parsedMinVolunteers < 0)
            {
                ValidationError = "Minimalan broj volontera mora biti pozitivan broj.";
                return;
            }

            DateOnly dateOnly = DateOnly.FromDateTime(Date.Value);
            TimeOnly timeOnly = TimeOnly.FromTimeSpan(parsedTime);

            Event newEvent = new Event(
                0,
                _neighborhoodId,
                _userId,
                Name.Trim(),
                Description?.Trim() ?? string.Empty,
                dateOnly,
                timeOnly,
                parsedDuration,
                parsedMinVolunteers,
                EventStatus.Preparation
            );

            List<string> requiredItems = new List<string>();
            if (!string.IsNullOrWhiteSpace(ItemsRawText))
            {
                string[] items = ItemsRawText.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string item in items)
                {
                    requiredItems.Add(item.Trim());
                }
            }

            try
            {
                _eventService.CreateEvent(newEvent, requiredItems);

                if (_window != null)
                {
                    _window.DialogResult = true;
                    _window.Close();
                }
            }
            catch (Exception ex)
            {
                ValidationError = ex.Message;
            }
        }

        private void ExecuteClose(object parameter)
        {
            if (_window != null)
            {
                _window.DialogResult = false;
                _window.Close();
            }
        }
    }
}