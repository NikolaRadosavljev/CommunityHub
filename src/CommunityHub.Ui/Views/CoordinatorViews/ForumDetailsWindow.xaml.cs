using System.Windows;
using CommunityHub.Application.Domain;
using CommunityHub.Application.Services;
using CommunityHub.Application;

namespace CommunityHub.Ui.Views.CoordinatorViews;

public partial class ForumDetailsWindow : Window
{
    private readonly long _userId;
    private readonly Forum _forum;
    private readonly ForumService _forumService;
    private List<ForumComment> _comments;

    public ForumDetailsWindow(long userId, Forum forum)
    {
        InitializeComponent();
        _userId = userId;
        _forum = forum;
        _forumService = Injector.CreateInstance<ForumService>();
        _comments = new List<ForumComment>();

        LoadForumInfo();
        LoadComments();

        if (_forum.CoordinatorId != _userId || _forum.IsClosed)
            CloseForumButton.IsEnabled = false;
    }

    private void LoadForumInfo()
    {
        ForumTitleTextBlock.Text = _forum.Title;
        ForumDescriptionTextBlock.Text = _forum.Description;
        ForumStatusTextBlock.Text = _forum.IsClosed ? "Status: Zatvoren" : "Status: Otvoren";
    }

    private void LoadComments()
    {
        _comments = _forumService.GetComments(_forum.Id);

        var view = _comments.Select(c => new
        {
            Id = c.Id,
            CoordinatorFullName = c.CoordinatorFullName,
            NeighborhoodName = c.NeighborhoodName,
            Content = c.Content,
            CreatedAt = c.CreatedAt.ToString("dd.MM.yyyy HH:mm"),
            AuthorTag = c.CoordinatorId == _forum.CoordinatorId ? "author" : "",
            Likes = c.Likes,
            Dislikes = c.Dislikes
        }).ToList();

        CommentsDataGrid.ItemsSource = view;
    }

    private void AddCommentButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            string content = NewCommentTextBox.Text;
            _forumService.AddComment(_forum.Id, _userId, content);

            MessageBox.Show("Komentar uspešno dodat.", "Uspeh",
                MessageBoxButton.OK, MessageBoxImage.Information);

            NewCommentTextBox.Clear();
            LoadComments();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Greška",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void LikeButton_Click(object sender, RoutedEventArgs e)
    {
        ReactToSelected(true);
    }

    private void DislikeButton_Click(object sender, RoutedEventArgs e)
    {
        ReactToSelected(false);
    }

    private void ReactToSelected(bool isPositive)
    {
        try
        {
            if (CommentsDataGrid.SelectedItem == null)
            {
                MessageBox.Show("Odaberite komentar.", "Greška",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            long commentId = (long)CommentsDataGrid.SelectedItem
                .GetType()
                .GetProperty("Id")!
                .GetValue(CommentsDataGrid.SelectedItem)!;

            _forumService.ReactToComment(commentId, _userId, isPositive);
            LoadComments();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Greška",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void CloseForumButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            MessageBoxResult result = MessageBox.Show(
                "Da li ste sigurni da želite da zatvorite forum?",
                "Potvrda", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            _forumService.CloseForum(_forum.Id, _userId);

            MessageBox.Show("Forum je zatvoren.", "Uspeh",
                MessageBoxButton.OK, MessageBoxImage.Information);

            ForumsWindow forumsWindow = new ForumsWindow(_userId);
            forumsWindow.Show();
            this.Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Greška",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BackButton_Click(object sender, RoutedEventArgs e)
    {
        ForumsWindow forumsWindow = new ForumsWindow(_userId);
        forumsWindow.Show();
        this.Close();
    }
}