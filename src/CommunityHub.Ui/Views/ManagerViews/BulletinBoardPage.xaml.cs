using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using CommunityHub.Application.Database.Repositories;
using CommunityHub.Application.Services.HelpAds;
using CommunityHub.Application.Services.HelpAds.DTO;
using CommunityHub.Ui.Security;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace CommunityHub.Ui.Views
{
    public partial class BulletinBoardPage : Page
    {
        private readonly long _managerId;
        private readonly int _buildingId;
        private readonly ManagerHelpBoardService _managerHelpBoardService;
        private readonly BuildingDbRepository _buildingRepository;
        private List<ManagerHelpAdOverviewDTO> _allAds;
        private string _buildingLabel = "Building";

        public BulletinBoardPage(int buildingId)
        {
            InitializeComponent();
            _managerId = UserSession.CurrentUser!.Id;
            _buildingId = buildingId;
            _managerHelpBoardService = new ManagerHelpBoardService();
            _buildingRepository = new BuildingDbRepository();
            _allAds = [];

            QuestPDF.Settings.License = LicenseType.Community;

            LoadHeader();
            LoadYearOptions();
            LoadData();
            ActivateAdsTab();
        }

        private void LoadHeader()
        {
            var building = _buildingRepository.GetById(_buildingId);
            _buildingLabel = building != null ? $"{building.Address}, {building.Settlement}" : "Building";
            TxtBuildingInfo.Text = _buildingLabel;
        }

        private void LoadYearOptions()
        {
            int currentYear = DateTime.Today.Year;
            for (int year = currentYear - 2; year <= currentYear + 2; year++)
                CbYear.Items.Add(year);
            CbYear.SelectedItem = currentYear;
        }

        private void LoadData()
        {
            _allAds = _managerHelpBoardService.GetAdsForBuilding(_managerId, _buildingId);
            ApplyAdsFilter();
            LoadStatistics();
            ApplyReportFilter();
        }

        // ---------------------------------------------------------------
        // ADS TAB
        // ---------------------------------------------------------------
        private void ApplyAdsFilter()
        {
            string statusFilter = (CbStatusFilter.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "All statuses";

            IEnumerable<ManagerHelpAdOverviewDTO> filtered = _allAds;
            if (statusFilter.Equals("Active", StringComparison.OrdinalIgnoreCase))
                filtered = filtered.Where(ad => ad.IsCurrentlyActive);
            else if (statusFilter.Equals("Archived", StringComparison.OrdinalIgnoreCase))
                filtered = filtered.Where(ad => ad.Status.Equals("ARCHIVED", StringComparison.OrdinalIgnoreCase));

            var items = filtered.Select(ad => new BulletinBoardAdDisplayItem
            {
                TypeLabel = ad.Type.Equals("offering", StringComparison.OrdinalIgnoreCase) ? "Offers Help" : "Needs Help",
                TenantLabel = ad.TenantName,
                CategoryLabel = ad.Category,
                DateRangeLabel = $"{ad.StartDate:dd.MM.yyyy} - {ad.EndDate:dd.MM.yyyy}",
                StatusLabel = ad.IsCurrentlyActive ? "Active" : "Archived"
            }).ToList();

            AdsItemsControl.ItemsSource = items;
            TxtEmptyAds.Visibility = items.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        // ---------------------------------------------------------------
        // STATISTICS TAB
        // ---------------------------------------------------------------
        private void LoadStatistics()
        {
            if (CbYear.SelectedItem == null) return;

            int selectedYear = (int)CbYear.SelectedItem;
            bool monthlyMode = ((CbPeriodType.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Year") == "Month";
            int? selectedMonth = null;

            if (monthlyMode && CbMonth.SelectedItem is ComboBoxItem monthItem && monthItem.Tag != null)
            {
                if (int.TryParse(monthItem.Tag.ToString(), out int monthValue))
                    selectedMonth = monthValue;
            }

            var statistics = _managerHelpBoardService.GetStatisticsForBuilding(_managerId, _buildingId, selectedYear, selectedMonth);

            TxtOffersCount.Text = statistics.OfferingCreatedCount.ToString();
            TxtNeedsCount.Text = statistics.SeekingCreatedCount.ToString();
            TxtActiveNowCount.Text = statistics.ActiveCount.ToString();
            TxtArchivedNowCount.Text = statistics.ArchivedCount.ToString();

            CategorySplitItemsControl.ItemsSource = statistics.CategorySplitCounts
                .Select(c => new CategorySplitDisplayItem { Category = c.Category, SummaryLabel = $"{c.OfferingCount} / {c.SeekingCount}" })
                .ToList();

            ActiveByCategoryItemsControl.ItemsSource = statistics.ActiveByCategory;

            TxtMostHelpfulTenant.Text = statistics.MostHelpfulTenant == null
                ? "No booked slots yet."
                : $"{statistics.MostHelpfulTenant.TenantName} ({statistics.MostHelpfulTenant.BookedSlotsCount} booked slots)";
        }

        // ---------------------------------------------------------------
        // REPORT TAB
        // ---------------------------------------------------------------
        private void ApplyReportFilter()
        {
            if (CbReportTypeFilter == null) return;

            string typeFilter = (CbReportTypeFilter.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "All";

            var activeAds = _allAds.Where(ad => ad.IsCurrentlyActive);

            if (typeFilter == "Offers Help")
                activeAds = activeAds.Where(ad => ad.Type.Equals("offering", StringComparison.OrdinalIgnoreCase));
            else if (typeFilter == "Needs Help")
                activeAds = activeAds.Where(ad => !ad.Type.Equals("offering", StringComparison.OrdinalIgnoreCase));

            var items = activeAds.Select(ad => new BulletinBoardAdDisplayItem
            {
                TypeLabel = ad.Type.Equals("offering", StringComparison.OrdinalIgnoreCase) ? "Offers Help" : "Needs Help",
                TenantLabel = ad.TenantName,
                CategoryLabel = ad.Category,
                DateRangeLabel = $"{ad.StartDate:dd.MM.yyyy} - {ad.EndDate:dd.MM.yyyy}",
                StatusLabel = "Active"
            }).ToList();

            ReportAdsItemsControl.ItemsSource = items;
            TxtReportShowing.Text = $"Showing: {typeFilter} ({items.Count})";
            TxtReportTotal.Text = $"Total: {items.Count} active ads";
        }

        private void GenerateReport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string typeFilter = (CbReportTypeFilter.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "All";

                var activeAds = _allAds.Where(ad => ad.IsCurrentlyActive);
                if (typeFilter == "Offers Help")
                    activeAds = activeAds.Where(ad => ad.Type.Equals("offering", StringComparison.OrdinalIgnoreCase));
                else if (typeFilter == "Needs Help")
                    activeAds = activeAds.Where(ad => !ad.Type.Equals("offering", StringComparison.OrdinalIgnoreCase));

                var adsList = activeAds.ToList();

                string fileName = $"BulletinBoard_Report_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
                string filePath = Path.Combine(Path.GetTempPath(), fileName);

                Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4);
                        page.Margin(30);
                        page.DefaultTextStyle(x => x.FontSize(11));

                        page.Header().Column(col =>
                        {
                            col.Item().Text("Bulletin Board Report").Bold().FontSize(20).AlignCenter();
                            col.Item().Text($"Active Ads on Board - {_buildingLabel}").FontSize(13).AlignCenter();
                            col.Item().Text($"Generated: {DateTime.Now:dd.MM.yyyy HH:mm}  |  Filter: {typeFilter}").FontSize(10).FontColor(Colors.Grey.Medium).AlignCenter();
                            col.Item().PaddingBottom(10).LineHorizontal(1).LineColor(Colors.Black);
                        });

                        page.Content().Column(col =>
                        {
                            col.Item().Text($"Total active ads: {adsList.Count}").Bold().FontSize(12);
                            col.Item().PaddingBottom(8);

                            if (adsList.Count == 0)
                            {
                                col.Item().Text("No active ads found for the selected filter.").Italic().FontColor(Colors.Grey.Medium);
                            }
                            else
                            {
                                col.Item().Table(table =>
                                {
                                    table.ColumnsDefinition(columns =>
                                    {
                                        columns.RelativeColumn(2); // Tenant
                                        columns.RelativeColumn(1.5f); // Type
                                        columns.RelativeColumn(1.5f); // Category
                                        columns.RelativeColumn(2); // Date Range
                                    });

                                    // Header
                                    table.Header(header =>
                                    {
                                        header.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Tenant").Bold();
                                        header.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Type").Bold();
                                        header.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Category").Bold();
                                        header.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Date Range").Bold();
                                    });

                                    foreach (var ad in adsList)
                                    {
                                        string type = ad.Type.Equals("offering", StringComparison.OrdinalIgnoreCase) ? "Offers Help" : "Needs Help";
                                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(ad.TenantName);
                                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(type);
                                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(ad.Category);
                                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4).Text($"{ad.StartDate:dd.MM.yyyy} - {ad.EndDate:dd.MM.yyyy}");
                                    }
                                });
                            }
                        });

                        page.Footer().AlignCenter().Text(x =>
                        {
                            x.Span("CommunityHub - Page ");
                            x.CurrentPageNumber();
                            x.Span(" / ");
                            x.TotalPages();
                        });
                    });
                }).GeneratePdf(filePath);

                // Open PDF
                Process.Start(new ProcessStartInfo(filePath) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                ManagerMainWindow.GetFrom(this)?.ShowErrorToast($"Error generating report: {ex.Message}");
            }
        }

        // ---------------------------------------------------------------
        // TAB SWITCHING
        // ---------------------------------------------------------------
        private void ActivateAdsTab()
        {
            AdsPanel.Visibility = Visibility.Visible;
            StatisticsPanel.Visibility = Visibility.Collapsed;
            ReportPanel.Visibility = Visibility.Collapsed;
            BtnAdsTab.FontWeight = FontWeights.Bold;
            BtnStatisticsTab.FontWeight = FontWeights.Normal;
            BtnReportTab.FontWeight = FontWeights.Normal;
        }

        private void ActivateStatisticsTab()
        {
            AdsPanel.Visibility = Visibility.Collapsed;
            StatisticsPanel.Visibility = Visibility.Visible;
            ReportPanel.Visibility = Visibility.Collapsed;
            BtnAdsTab.FontWeight = FontWeights.Normal;
            BtnStatisticsTab.FontWeight = FontWeights.Bold;
            BtnReportTab.FontWeight = FontWeights.Normal;
            LoadStatistics();
        }

        private void ActivateReportTab()
        {
            AdsPanel.Visibility = Visibility.Collapsed;
            StatisticsPanel.Visibility = Visibility.Collapsed;
            ReportPanel.Visibility = Visibility.Visible;
            BtnAdsTab.FontWeight = FontWeights.Normal;
            BtnStatisticsTab.FontWeight = FontWeights.Normal;
            BtnReportTab.FontWeight = FontWeights.Bold;
            ApplyReportFilter();
        }

        private void AdsTab_Click(object sender, RoutedEventArgs e) => ActivateAdsTab();
        private void StatisticsTab_Click(object sender, RoutedEventArgs e) => ActivateStatisticsTab();
        private void ReportTab_Click(object sender, RoutedEventArgs e) => ActivateReportTab();

        private void CbStatusFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded) return;
            ApplyAdsFilter();
        }

        private void CbPeriodType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded || CbMonth == null || CbPeriodType == null) return;
            bool monthlyMode = ((CbPeriodType.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Year") == "Month";
            CbMonth.Visibility = monthlyMode ? Visibility.Visible : Visibility.Collapsed;
            LoadStatistics();
        }

        private void StatisticsFilter_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded) return;
            LoadStatistics();
        }

        private void ReportTypeFilter_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded) return;
            ApplyReportFilter();
        }
    }

    public class BulletinBoardAdDisplayItem
    {
        public string TypeLabel { get; set; } = string.Empty;
        public string TenantLabel { get; set; } = string.Empty;
        public string CategoryLabel { get; set; } = string.Empty;
        public string DateRangeLabel { get; set; } = string.Empty;
        public string StatusLabel { get; set; } = string.Empty;
    }

    public class CategorySplitDisplayItem
    {
        public string Category { get; set; } = string.Empty;
        public string SummaryLabel { get; set; } = string.Empty;
    }
}
