using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using CommunityHub.Application.Domain;
using CommunityHub.Application.Services;
using CommunityHub.Ui.Commands;
using CommunityHub.Ui.Views.CitizenViews;

namespace CommunityHub.Ui.ViewModels.CitizenViewModels
{
    public class NeighborhoodsViewModel : ViewModelBase
    {
        private readonly long _userId;
        private readonly NeighborhoodService _neighborhoodService;
        private readonly NeighborhoodMembershipService _membershipService;

        private string _nameFilter;
        public string NameFilter { get => _nameFilter; set { _nameFilter = value; OnPropertyChanged(); } }

        private string _addressFilter;
        public string AddressFilter { get => _addressFilter; set { _addressFilter = value; OnPropertyChanged(); } }

        public ObservableCollection<City> Cities { get; set; }
        public ObservableCollection<Country> Countries { get; set; }

        private City _selectedCity;
        public City SelectedCity { get => _selectedCity; set { _selectedCity = value; OnPropertyChanged(); } }

        private Country _selectedCountry;
        public Country SelectedCountry { get => _selectedCountry; set { _selectedCountry = value; OnPropertyChanged(); } }

        public ObservableCollection<Neighborhood> Neighborhoods { get; set; }

        public ICommand SearchCommand { get; }
        public ICommand RequestAccessCommand { get; }
        public ICommand ViewRequestsCommand { get; } 
        public ICommand BackCommand { get; }

        public NeighborhoodsViewModel(long userId)
        {
            _userId = userId;
            _neighborhoodService = new NeighborhoodService();
            _membershipService = new NeighborhoodMembershipService();

            Neighborhoods = new ObservableCollection<Neighborhood>();
            Cities = new ObservableCollection<City>();
            Countries = new ObservableCollection<Country>();

            SearchCommand = new RelayCommand(o => ExecuteSearch());
            RequestAccessCommand = new RelayCommand(o => ExecuteRequestAccess(o));
            ViewRequestsCommand = new RelayCommand(o => ExecuteViewRequests()); 
            BackCommand = new RelayCommand(o => ExecuteBack());

            LoadDropdownData();
            ExecuteSearch();
        }

        private void LoadDropdownData()
        {
            var allNeighborhoods = _neighborhoodService.Search(null, null, null, null);
            if (allNeighborhoods == null) return;

            var uniqueCities = allNeighborhoods
                .Select(n => n.City)
                .Where(c => c != null)
                .GroupBy(c => c.Name)
                .Select(g => g.First());

            foreach (var city in uniqueCities) Cities.Add(city);

            var uniqueCountries = allNeighborhoods
                .Select(n => n.City?.Country)
                .Where(c => c != null)
                .GroupBy(c => c.Name)
                .Select(g => g.First());

            foreach (var country in uniqueCountries) Countries.Add(country);
        }

        private void ExecuteSearch()
        {
            string name = string.IsNullOrWhiteSpace(NameFilter) ? null : NameFilter;
            string address = string.IsNullOrWhiteSpace(AddressFilter) ? null : AddressFilter;

            string city = SelectedCity?.Name;
            string country = SelectedCountry?.Name;

            var results = _neighborhoodService.Search(name, address, city, country);
            Neighborhoods.Clear();
            if (results != null)
            {
                foreach (var item in results) Neighborhoods.Add(item);
            }
        }

        private void ExecuteRequestAccess(object param)
        {
            if (param is Neighborhood neighborhood)
            {
                string result = _membershipService.RequestAccess(_userId, neighborhood);
                MessageBox.Show(result);
            }
        }

        private void ExecuteViewRequests()
        {
            var target = new NeighborhoodMembershipRequestViewWindow(_userId);
            target.ShowDialog();
        }

        private void ExecuteBack()
        {
            var home = new CitizenHomeWindow(_userId);
            home.Show();
            foreach (System.Windows.Window window in System.Windows.Application.Current.Windows)
            {
                if (window != home && window.IsVisible)
                {
                    window.Close();
                }
            }
        }
    }
}