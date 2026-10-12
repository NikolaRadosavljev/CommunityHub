using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityHub.Application.Domain;
using CommunityHub.Application.Services;
using CommunityHub.Ui.Commands;
using CommunityHub.Application;

namespace CommunityHub.Ui.ViewModels.CitizenViewModels
{
    public class CitizenTrustRecordItemViewModel : ViewModelBase
    {
        public string FullName { get; }
        public string JoinedDate { get; }
        public string TrustLevelName { get; }

        public CitizenTrustRecordItemViewModel(Citizen citizen, TrustRecord record)
        {
            FullName = $"{citizen.User.Name} {citizen.User.Surname}";
            JoinedDate = record.JoinedAt.ToString("dd. MM. yyyy.");
            TrustLevelName = record.LevelDisplayName;
        }
    }

    public class NeighborhoodTrustRecordsViewModel : ViewModelBase
    {
        private readonly long _neighborhoodId;
        private readonly TrustRecordService _trustRecordService;
        private readonly Action _closeAction;

        public ObservableCollection<CitizenTrustRecordItemViewModel> Records { get; }
        public ICommand CloseCommand { get; }

        public NeighborhoodTrustRecordsViewModel(long neighborhoodId, Action closeAction)
        {
            _neighborhoodId = neighborhoodId;
            _closeAction = closeAction;
            _trustRecordService = Injector.CreateInstance<TrustRecordService>();
            Records = new ObservableCollection<CitizenTrustRecordItemViewModel>();

            CloseCommand = new RelayCommand(o => _closeAction?.Invoke());

            LoadRecords();
        }

        private void LoadRecords()
        {
            var dynamicData = _trustRecordService.GetForNeighborhood(_neighborhoodId);

            Records.Clear();
            foreach (var item in dynamicData)
            {
                Records.Add(new CitizenTrustRecordItemViewModel(item.citizen, item.record));
            }
        }
    }
}