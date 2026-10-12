using System.Windows;
using CommunityHub.Application.Database.Repositories;
using CommunityHub.Application.Domain;


namespace CommunityHub.Ui.Views;

public partial class CreatePostWindow : Window
{
    private readonly long _userId;
    private readonly PostDbRepository _postRepository;

    public Post? CreatedPost { get; private set; }

    public CreatePostWindow(long userId)
    {
        InitializeComponent();
        _userId = userId;
        _postRepository = new PostDbRepository();
    }

    private void TitleTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        bool isValid = TitleTextBox.Text.Length >= 3;
        SaveButton.IsEnabled = isValid;
        ValidationErrorTextBlock.Visibility = TitleTextBox.Text.Length > 0 && !isValid
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        Post post = new Post(TitleTextBox.Text, ContentTextBox.Text);
        CreatedPost = _postRepository.Create(post, _userId);

        DialogResult = true;
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
