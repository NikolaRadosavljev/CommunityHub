using System.Windows;
using CommunityHub.Application.Domain;
using CommunityHub.Application.Services;
using CommunityHub.Application;

namespace CommunityHub.Ui.Views.CoordinatorViews;

public partial class ForumsWindow : Window
{
    private readonly long _userId;
    private readonly ForumService _forumService;
    private List<Forum> _forums;

    public ForumsWindow(long userId)
    {
        InitializeComponent();
        _userId = userId;
        _forumService = Injector.CreateInstance<ForumService>();
        _forums = new List<Forum>();

        LoadForums();
    }

    private void LoadForums()
    {
        _forums = _forumService.GetVisibleForums(_userId, null, null);
        RefreshDataGrid(_forums);
    }

    private void RefreshDataGrid(List<Forum> forums)
    {
        var view = forums.Select(f => new
        {
            Id = f.Id,
            Title = f.Title,
            Description = f.Description,
            Status = f.IsClosed ? "Zatvoren" : "Otvoren"
        }).ToList();

        ForumsDataGrid.ItemsSource = view;
    }

    private void SearchButton_Click(object sender, RoutedEventArgs e)
    {
        string? title = string.IsNullOrWhiteSpace(TitleSearchTextBox.Text)
            ? null : TitleSearchTextBox.Text;
        string? description = string.IsNullOrWhiteSpace(DescriptionSearchTextBox.Text)
            ? null : DescriptionSearchTextBox.Text;

        _forums = _forumService.GetVisibleForums(_userId, title, description);
        RefreshDataGrid(_forums);
    }

    private void CreateForumButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            string title = NewTitleTextBox.Text;
            string description = NewDescriptionTextBox.Text;

            _forumService.CreateForum(_userId, title, description);

            MessageBox.Show("Forum uspešno kreiran.", "Uspeh",
                MessageBoxButton.OK, MessageBoxImage.Information);

            NewTitleTextBox.Clear();
            NewDescriptionTextBox.Clear();
            LoadForums();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Greška",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void OpenForumButton_Click(object sender, RoutedEventArgs e)
    {
        if (ForumsDataGrid.SelectedItem == null)
        {
            MessageBox.Show("Odaberite forum.", "Greška",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        long forumId = (long)ForumsDataGrid.SelectedItem
            .GetType()
            .GetProperty("Id")!
            .GetValue(ForumsDataGrid.SelectedItem)!;

        Forum? selected = _forums.FirstOrDefault(f => f.Id == forumId);
        if (selected == null) return;

        ForumDetailsWindow detailsWindow = new ForumDetailsWindow(_userId, selected);
        detailsWindow.Show();
        this.Close();
    }

    private void BackButton_Click(object sender, RoutedEventArgs e)
    {
        HomeWindow homeWindow = new HomeWindow(_userId);
        homeWindow.Show();
        this.Close();
    }
}