using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using CommunityHub.Application.Domain;
using CommunityHub.Application.Domain.RepositoryInterfaces;
using CommunityHub.Application.Services;
using CommunityHub.Ui.Infrastructure;
using CommunityHub.Ui.Security;
using CommunityHub.Ui.ViewModels.ManagerViewModels;

namespace CommunityHub.Ui.Views.ManagerViews
{
    public partial class UnresolvedProblemsPage : Page
    {
        private readonly UnresolvedProblemsViewModel? _viewModel;

        public UnresolvedProblemsPage()
        {
            InitializeComponent();

            if (!System.ComponentModel.DesignerProperties.GetIsInDesignMode(this))
            {
                _viewModel = new UnresolvedProblemsViewModel();
                DataContext = _viewModel;
                this.Loaded += async (s, e) => await LoadUnresolvedProblemsAsync();
            }
        }

        private async Task LoadUnresolvedProblemsAsync()
        {
            if (_viewModel == null) return;

            await _viewModel.LoadUnresolvedProblemsAsync();

            SpProblems.Children.Clear();

            if (_viewModel.BuildingProblems.Count == 0)
            {
                TbEmpty.Visibility = Visibility.Visible;
                SpProblems.Children.Add(TbEmpty);
                return;
            }

            foreach (var group in _viewModel.BuildingProblems)
            {
                // Building header
                SpProblems.Children.Add(new TextBlock
                {
                    Text = group.BuildingLabel,
                    FontSize = 16,
                    FontWeight = FontWeights.Bold,
                    Margin = new Thickness(0, 10, 0, 8)
                });

                // Separator
                SpProblems.Children.Add(new Border
                {
                    BorderBrush = System.Windows.Media.Brushes.Black,
                    BorderThickness = new Thickness(0, 0, 0, 1),
                    Margin = new Thickness(0, 0, 0, 8)
                });

                foreach (var problem in group.Problems)
                {
                    // Build a simple card for each unresolved problem using the display item data
                    var card = BuildUnresolvedProblemCard(problem);
                    SpProblems.Children.Add(card);
                }
            }
        }

        private Border BuildUnresolvedProblemCard(ProblemDisplayItem p)
        {
            var border = new Border
            {
                BorderThickness = new Thickness(1),
                BorderBrush = System.Windows.Media.Brushes.Black,
                Margin = new Thickness(0, 0, 0, 10),
                Padding = new Thickness(10)
            };

            var stack = new StackPanel();

            // Title row + status
            var headerGrid = new Grid();
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var titleText = new TextBlock { Text = "Problem", FontSize = 14, FontWeight = FontWeights.Bold };
            Grid.SetColumn(titleText, 0);
            headerGrid.Children.Add(titleText);

            var orangeBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 165, 0));
            var statusBorder = new Border
            {
                BorderBrush = orangeBrush,
                BorderThickness = new Thickness(1),
                Padding = new Thickness(8, 2, 8, 2)
            };
            statusBorder.Child = new TextBlock { Text = p.StatusLabel, FontSize = 11, Foreground = orangeBrush, FontWeight = FontWeights.SemiBold };
            Grid.SetColumn(statusBorder, 1);
            headerGrid.Children.Add(statusBorder);
            stack.Children.Add(headerGrid);

            // Tenant | Date
            stack.Children.Add(new TextBlock
            {
                Text = $"{p.TenantName}  |  {p.ReportedDate:dd.MM.yyyy}",
                FontSize = 12,
                Margin = new Thickness(0, 4, 0, 2)
            });

            // Priority
            stack.Children.Add(new TextBlock
            {
                Text = $"Priority: {p.PriorityLabel}",
                FontSize = 12,
                Margin = new Thickness(0, 0, 0, 2)
            });

            // Description
            stack.Children.Add(new TextBlock
            {
                Text = $"\"{p.Description}\"",
                FontSize = 12,
                FontStyle = FontStyles.Italic,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 2, 0, 6)
            });

            // Mark as Resolved button
            if (p.Status != "Resolved" && p.Status != "Potentially_resolved")
            {
                var resolveBtn = new Button
                {
                    Content = "Mark as Resolved",
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    Background = System.Windows.Media.Brushes.White,
                    Foreground = System.Windows.Media.Brushes.Black,
                    BorderBrush = System.Windows.Media.Brushes.Black,
                    BorderThickness = new Thickness(1),
                    Padding = new Thickness(8, 4, 8, 4),
                    FontSize = 12,
                    FontWeight = FontWeights.SemiBold,
                    Cursor = System.Windows.Input.Cursors.Hand,
                    Tag = p.Id
                };
                resolveBtn.Click += async (s, e) =>
                {
                    try
                    {
                        if (_viewModel != null)
                        {
                            await _viewModel.MarkAsResolvedAsync(p.Id);
                            await LoadUnresolvedProblemsAsync();
                        }
                    }
                    catch (Exception ex)
                    {
                        ManagerMainWindow.GetFrom(this)?.ShowErrorToast($"Error: {ex.Message}");
                    }
                };
                stack.Children.Add(resolveBtn);
            }
            else if (p.Status == "Potentially_resolved")
            {
                stack.Children.Add(new TextBlock
                {
                    Text = "Waiting for tenant confirmation",
                    FontSize = 11,
                    FontStyle = FontStyles.Italic,
                    Foreground = System.Windows.Media.Brushes.Gray
                });
            }

            border.Child = stack;
            return border;
        }
    }
}
