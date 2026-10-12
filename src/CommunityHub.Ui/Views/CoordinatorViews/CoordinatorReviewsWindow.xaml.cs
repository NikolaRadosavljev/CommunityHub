using System.Windows;
using CommunityHub.Application;
using CommunityHub.Application.Services;

namespace CommunityHub.Ui.Views.CoordinatorViews;

public partial class CoordinatorReviewsWindow : Window
{
    private readonly long _userId;
    private readonly CoordinatorReviewService _reviewService;

    public CoordinatorReviewsWindow(long userId)
    {
        InitializeComponent();
        _userId = userId;
        _reviewService = Injector.CreateInstance<CoordinatorReviewService>();

        LoadReviews();
    }

    private void LoadReviews()
    {
        var reviews = _reviewService.GetReviews(_userId);
        double average = _reviewService.GetAverageRatingLastYear(_userId);

        AverageRatingTextBlock.Text = $"Prosečna ocena u poslednjih godinu dana: {average:F1}";

        var view = reviews.Select(r => new
        {
            Rating = r.Rating,
            Comment = r.Comment ?? "-",
            CreatedAt = r.CreatedAt.ToString("dd.MM.yyyy")
        }).ToList();

        ReviewsDataGrid.ItemsSource = view;
    }

    private void BackButton_Click(object sender, RoutedEventArgs e)
    {
        HomeWindow homeWindow = new HomeWindow(_userId);
        homeWindow.Show();
        this.Close();
    }
}