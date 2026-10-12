using System.Windows;
using System.Windows.Controls;
using CommunityHub.Application.Domain;
using CommunityHub.Application.Services;
using CommunityHub.Application;
using CommunityHub.Ui.Views.CoordinatorViews;
using CommunityHub.Ui.Views.CoordinatorViews;


namespace CommunityHub.Ui.Views;

public partial class CitizenStatisticsWindow : Window
{
    private readonly long _userId;
    private readonly CitizenTrustService _trustService;
    private readonly MeetingService _meetingService;
    private readonly NeighborhoodService _neighborhoodService;
    private List<Neighborhood> _neighborhoods;
    private List<Meeting> _meetings;
    private long? _selectedMeetingId;

    public CitizenStatisticsWindow(long userId)
    {
        InitializeComponent();
        _userId = userId;
        _trustService = Injector.CreateInstance<CitizenTrustService>();
        _meetingService = Injector.CreateInstance<MeetingService>();
        _neighborhoodService = Injector.CreateInstance<NeighborhoodService>();
        _neighborhoods = new List<Neighborhood>();
        _meetings = new List<Meeting>();

        LoadTopics();
        LoadTimeComboBoxes();
        LoadNeighborhoods();
    }

    private void LoadTopics()
    {
        TopicComboBox.Items.Add("Welcome");
        TopicComboBox.Items.Add("Motivation");
        TopicComboBox.SelectedIndex = 0;
    }

    private void LoadTimeComboBoxes()
    {
        for (int h = 0; h < 24; h++)
            HourComboBox.Items.Add(h.ToString("00"));

        for (int m = 0; m < 60; m += 15)
            MinuteComboBox.Items.Add(m.ToString("00"));

        HourComboBox.SelectedIndex = 8;
        MinuteComboBox.SelectedIndex = 0;
    }

    private void LoadNeighborhoods()
    {
        _neighborhoods = _neighborhoodService.GetByCoordinatorId(_userId);
        NeighborhoodComboBox.ItemsSource = _neighborhoods;

        if (_neighborhoods.Count > 0)
            NeighborhoodComboBox.SelectedIndex = 0;
    }

    private void NeighborhoodComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (NeighborhoodComboBox.SelectedItem is not Neighborhood selected) return;

        LoadStatistics(selected.Id);
        LoadMeetings();
    }

    private void LoadStatistics(long neighborhoodId)
    {
        var distribution = _trustService.GetLevelDistribution(neighborhoodId);

        var view = distribution.Select(kvp => new
        {
            Level = kvp.Key.ToString(),
            Count = kvp.Value
        }).ToList();

        StatisticsDataGrid.ItemsSource = view;

        CitizenTrustLevel dominant = _trustService.GetDominantLevel(neighborhoodId);

        if (dominant == CitizenTrustLevel.New)
            RecommendationTextBlock.Text = "Preporuka sistema: Organizujte sastanak dobrodošlice — najveći broj građana je nov u kvartu.";
        else if (dominant == CitizenTrustLevel.Inactive)
            RecommendationTextBlock.Text = "Preporuka sistema: Organizujte motivacioni sastanak — najveći broj građana je neaktivan.";
        else
            RecommendationTextBlock.Text = "Trenutno nema posebne preporuke za sastanak.";
    }

    private void LoadMeetings()
    {
        _meetings = _meetingService.GetMeetingsForCoordinator(_userId);

        var view = _meetings.Select(m => new
        {
            Id = m.Id,
            Topic = m.Topic.ToString(),
            Time = m.Time.ToString("HH:mm"),
            StartDate = m.StartDate.ToString(),
            EndDate = m.EndDate.ToString(),
            Status = m.Status.ToString(),
            ScheduledDate = m.ScheduledDate?.ToString() ?? "-"
        }).ToList();

        MeetingsDataGrid.ItemsSource = view;
    }

    private void CreateMeetingButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (NeighborhoodComboBox.SelectedItem is not Neighborhood selected)
            {
                MessageBox.Show("Odaberite kvart.", "Greška",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (HourComboBox.SelectedItem == null || MinuteComboBox.SelectedItem == null)
            {
                MessageBox.Show("Odaberite vreme.", "Greška",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int hour = int.Parse(HourComboBox.SelectedItem.ToString()!);
            int minute = int.Parse(MinuteComboBox.SelectedItem.ToString()!);
            TimeOnly time = new TimeOnly(hour, minute);

            if (StartDatePicker.SelectedDate == null)
            {
                MessageBox.Show("Odaberite početni datum.", "Greška",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (EndDatePicker.SelectedDate == null)
            {
                MessageBox.Show("Odaberite krajnji datum.", "Greška",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            DateOnly startDate = DateOnly.FromDateTime(StartDatePicker.SelectedDate.Value);
            DateOnly endDate = DateOnly.FromDateTime(EndDatePicker.SelectedDate.Value);

            MeetingTopic topic = TopicComboBox.SelectedItem?.ToString() == "Welcome"
                ? MeetingTopic.Welcome
                : MeetingTopic.Motivation;

            _meetingService.CreateMeeting(selected.Id, topic, time, startDate, endDate);

            MessageBox.Show("Sastanak uspešno kreiran.", "Uspeh",
                MessageBoxButton.OK, MessageBoxImage.Information);

            LoadMeetings();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Greška",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void MeetingsDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (MeetingsDataGrid.SelectedItem == null)
        {
            HideTiePanel();
            return;
        }

        long meetingId = (long)MeetingsDataGrid.SelectedItem
            .GetType()
            .GetProperty("Id")!
            .GetValue(MeetingsDataGrid.SelectedItem)!;

        _selectedMeetingId = meetingId;

        Meeting? meeting = _meetings.FirstOrDefault(m => m.Id == meetingId);
        if (meeting == null || meeting.Status != MeetingStatus.InPreparation)
        {
            HideTiePanel();
            return;
        }

        List<DateOnly> tiedDates = _meetingService.GetTiedDates(meetingId);
        if (tiedDates.Count >= 2)
        {
            TieTextBlock.Text = "Postoji izjednačen broj glasova. Odaberite datum:";
            TieTextBlock.Visibility = Visibility.Visible;
            TiedDatesListBox.ItemsSource = tiedDates.Select(d => d.ToString()).ToList();
            TiedDatesListBox.Visibility = Visibility.Visible;
            ManualScheduleButton.Visibility = Visibility.Visible;
        }
        else
        {
            HideTiePanel();
        }
    }

    private void ManualScheduleButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (_selectedMeetingId == null)
            {
                MessageBox.Show("Nije odabran sastanak.", "Greška",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (TiedDatesListBox.SelectedItem == null)
            {
                MessageBox.Show("Odaberite datum iz liste.", "Greška",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            DateOnly chosenDate = DateOnly.Parse(TiedDatesListBox.SelectedItem.ToString()!);
            _meetingService.ManuallySchedule(_selectedMeetingId.Value, chosenDate);

            MessageBox.Show("Sastanak uspešno zakazan.", "Uspeh",
                MessageBoxButton.OK, MessageBoxImage.Information);

            HideTiePanel();
            LoadMeetings();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Greška",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void HideTiePanel()
    {
        TieTextBlock.Visibility = Visibility.Collapsed;
        TiedDatesListBox.Visibility = Visibility.Collapsed;
        ManualScheduleButton.Visibility = Visibility.Collapsed;
    }

    private void BackButton_Click(object sender, RoutedEventArgs e)
    {
        HomeWindow homeWindow = new HomeWindow(_userId);
        homeWindow.Show();
        this.Close();
    }
}