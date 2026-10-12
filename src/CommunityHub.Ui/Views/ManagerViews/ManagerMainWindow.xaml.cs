using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Navigation;
using CommunityHub.Application.Domain;
using CommunityHub.Application.Domain.RepositoryInterfaces;
using CommunityHub.Ui.Infrastructure;
using CommunityHub.Ui.Security;
using CommunityHub.Ui.Views.ManagerViews;
using CommunityHub.Ui.Views.ManagerViews.Assemblies;

namespace CommunityHub.Ui.Views;

public partial class ManagerMainWindow : Window
{
    private Page? _currentPage;
    private CancellationTokenSource? _demoCts;
    private bool _isDemoRunning;

    private static readonly SolidColorBrush HighlightBg = new(Color.FromRgb(200, 200, 200));
    private const int TYPING_DELAY = 100;
    private const int CLICK_DELAY = 400;
    private const int PAUSE_AFTER_HIGHLIGHT = 1000;
    private const int PAUSE_BETWEEN_STEPS = 800;

    public ManagerMainWindow() : this(0) { }

    public ManagerMainWindow(long userId)
    {
        InitializeComponent();

        if (!UserSession.IsAuthenticated)
        {
            RedirectToLogin();
            return;
        }

        MainFrame.Navigate(new ManagerHomePage());

        this.Loaded += (s, e) =>
        {
            this.WindowState = WindowState.Normal;
            this.Activate();
            this.Topmost = true;
            this.Topmost = false;
            this.Focus();
        };

        // Block keyboard input during demo
        this.PreviewKeyDown += (s, e) =>
        {
            if (_isDemoRunning) { e.Handled = true; }
        };

        // Block mouse clicks during demo (except on the STOP button in header)
        this.PreviewMouseDown += (s, e) =>
        {
            if (_isDemoRunning)
            {
                // Allow clicking the STOP button
                if (e.OriginalSource is DependencyObject source && IsChildOf(source, BtnHeaderAction))
                    return;
                e.Handled = true;
            }
        };
    }

    public void NavigateToCommonRooms(int buildingId)
    {
        MainFrame.Navigate(new CommonRoomsPage(buildingId));
    }

    // ---------------------------------------------------------------
    // Header action
    // ---------------------------------------------------------------
    private void HeaderAction_Click(object sender, RoutedEventArgs e)
    {
        if (_isDemoRunning) { StopDemoMode(); return; }
        if (_currentPage is CreateBuildingPage cbp) { cbp.StartDemo(); return; }
        if (_currentPage is AddCommonSpacePage acsp) { acsp.StartDemo(); return; }
        if (_currentPage is RejectBuildingRequestPage rbp) { rbp.StartDemo(); return; }
        if (_currentPage is ScheduleAssemblyPage sap) { sap.StartDemo(); return; }
        SideMenuOverlay.Visibility = Visibility.Visible;
    }

    // ---------------------------------------------------------------
    // DEMO MODE
    // ---------------------------------------------------------------
    private async void SideNav_DemoModeClick(object sender, RoutedEventArgs e)
    {
        SideMenuOverlay.Visibility = Visibility.Collapsed;
        await StartDemoModeAsync();
    }

    private void StopDemoMode() => _demoCts?.Cancel();

    private void ShowDemoStopButton()
    {
        _origHeaderContent = BtnHeaderAction.Content;
        _origHeaderBg = BtnHeaderAction.Background;
        _origHeaderFg = BtnHeaderAction.Foreground;
        _origHeaderBorder = BtnHeaderAction.BorderBrush;
        _origHeaderBorderThickness = BtnHeaderAction.BorderThickness;
        _origHeaderFontSize = BtnHeaderAction.FontSize;

        BtnHeaderAction.Content = "STOP";
        BtnHeaderAction.Background = Brushes.Red;
        BtnHeaderAction.Foreground = Brushes.White;
        BtnHeaderAction.BorderBrush = Brushes.DarkRed;
        BtnHeaderAction.BorderThickness = new Thickness(1);
        BtnHeaderAction.FontSize = 12;
        BtnHeaderAction.Visibility = Visibility.Visible;
    }

    private void HideDemoStopButton()
    {
        BtnHeaderAction.Content = _origHeaderContent ?? "☰";
        BtnHeaderAction.Background = _origHeaderBg ?? Brushes.Transparent;
        BtnHeaderAction.Foreground = _origHeaderFg ?? Brushes.White;
        BtnHeaderAction.BorderBrush = _origHeaderBorder ?? Brushes.Transparent;
        BtnHeaderAction.BorderThickness = _origHeaderBorderThickness;
        BtnHeaderAction.FontSize = _origHeaderFontSize;
    }

    private object? _origHeaderContent;
    private Brush? _origHeaderBg;
    private Brush? _origHeaderFg;
    private Brush? _origHeaderBorder;
    private Thickness _origHeaderBorderThickness;
    private double _origHeaderFontSize;

    private async Task StartDemoModeAsync()
    {
        if (_isDemoRunning) return;
        _isDemoRunning = true;
        _demoCts = new CancellationTokenSource();
        var token = _demoCts.Token;

        ShowDemoStopButton();

        var buildingRepo = ManagerDependencyInjector.CreateInstance<IBuildingRepository>();
        var buildings = buildingRepo.GetByManagerId(UserSession.CurrentUser!.Id);
        int firstBuildingId = buildings.Count > 0 ? buildings[0].Id : 0;

        try
        {
          while (!token.IsCancellationRequested)
          {
            // =====================================================
            // Start on MyBuildings
            // =====================================================
            MainFrame.Navigate(new ManagerHomePage());
            await Task.Delay(1500, token);

            // Highlight "+Register new building" button and click
            var homePage = _currentPage as ManagerHomePage;
            if (homePage != null)
            {
                var registerBtn = FindButtonByContent(homePage, "+ Register new building");
                if (registerBtn != null)
                {
                    await SimulateButtonClick(registerBtn, token);
                }
            }

            // =====================================================
            // PAGE 1/2: Fill building registration
            // =====================================================
            var createPage = new CreateBuildingPage();
            MainFrame.Navigate(createPage);
            await Task.Delay(PAUSE_BETWEEN_STEPS, token);

            // Lock inputs for demo
            createPage.AddressTextBox.IsReadOnly = true;
            createPage.SettlementTextBox.IsReadOnly = true;
            createPage.FloorCountTextBox.IsReadOnly = true;
            createPage.CountryComboBox.IsEnabled = false;
            createPage.CityComboBox.IsEnabled = false;

            await TypeTextAsync(createPage.AddressTextBox, "Fruskogorska 10", token);
            await Task.Delay(PAUSE_BETWEEN_STEPS, token);

            await TypeTextAsync(createPage.SettlementTextBox, "Liman", token);
            await Task.Delay(PAUSE_BETWEEN_STEPS, token);

            if (createPage.CountryComboBox.Items.Count > 0)
            {
                createPage.CountryComboBox.IsEnabled = true;
                createPage.CountryComboBox.SelectedIndex = 0;
                createPage.CountryComboBox.IsEnabled = false;
                await Task.Delay(PAUSE_BETWEEN_STEPS, token);

                if (createPage.CityComboBox.Items.Count > 0)
                {
                    createPage.CityComboBox.IsEnabled = true;
                    createPage.CityComboBox.SelectedIndex = 0;
                    createPage.CityComboBox.IsEnabled = false;
                    await Task.Delay(PAUSE_BETWEEN_STEPS, token);
                }
            }

            await TypeTextAsync(createPage.FloorCountTextBox, "3", token);
            await Task.Delay(PAUSE_BETWEEN_STEPS, token);

            // Highlight "Next page" button and click
            var nextPageBtn = FindButtonByContent(createPage, "Next page »");
            if (nextPageBtn != null)
            {
                await SimulateButtonClick(nextPageBtn, token);
            }

            // =====================================================
            // PAGE 2/2: Floor flat counts
            // =====================================================
            // Setup floors manually (skip validation)
            createPage.Page1Grid.Visibility = Visibility.Collapsed;
            createPage.Page2Grid.Visibility = Visibility.Visible;

            var floorsList = new ObservableCollection<FloorViewModel>
            {
                new FloorViewModel { FloorNumber = 1, Title = "Floor 1:", FlatCount = 1 },
                new FloorViewModel { FloorNumber = 2, Title = "Floor 2:", FlatCount = 1 },
                new FloorViewModel { FloorNumber = 3, Title = "Floor 3:", FlatCount = 1 }
            };
            createPage.FloorsItemsControl.ItemsSource = floorsList;
            await Task.Delay(PAUSE_BETWEEN_STEPS, token);

            // Floor 1: clear and type "5"
            floorsList[0].FlatCount = 0;
            await Task.Delay(300, token);
            for (int i = 1; i <= 5; i++)
            {
                floorsList[0].FlatCount = i;
                await Task.Delay(250, token);
            }
            await Task.Delay(PAUSE_BETWEEN_STEPS, token);

            // Floor 2: click + from 1 to 7
            for (int i = 2; i <= 7; i++)
            {
                floorsList[1].FlatCount = i;
                await Task.Delay(CLICK_DELAY, token);
            }
            await Task.Delay(500, token);

            // Click - twice: 7 -> 6 -> 5
            floorsList[1].FlatCount = 6;
            await Task.Delay(CLICK_DELAY, token);
            floorsList[1].FlatCount = 5;
            await Task.Delay(CLICK_DELAY, token);

            // Floor 3: leave as is
            await Task.Delay(PAUSE_BETWEEN_STEPS, token);

            // Highlight "Finish Registration" button
            var finishRegBtn = FindButtonByContent(createPage, "Finish Registration");
            if (finishRegBtn != null)
            {
                await SimulateButtonClick(finishRegBtn, token);
            }

            // =====================================================
            // Back to MyBuildings → Common Rooms
            // =====================================================
            MainFrame.Navigate(new ManagerHomePage());
            await Task.Delay(1500, token);

            if (firstBuildingId > 0)
            {
                // Find and highlight "Common Rooms" button on first building card
                homePage = _currentPage as ManagerHomePage;
                if (homePage != null)
                {
                    var commonRoomsBtn = FindButtonByContent(homePage, "Common rooms");
                    if (commonRoomsBtn != null)
                    {
                        await SimulateButtonClick(commonRoomsBtn, token);
                    }
                }

                MainFrame.Navigate(new CommonRoomsPage(firstBuildingId));
                await Task.Delay(1200, token);

                // Find and highlight "+ Add Common Room"
                if (_currentPage is CommonRoomsPage crp)
                {
                    var addBtn = FindButtonByContent(crp, "+ Add Common Room");
                    if (addBtn != null)
                    {
                        await SimulateButtonClick(addBtn, token);
                    }
                }

                // =====================================================
                // Add Common Space demo
                // =====================================================
                var addCommonPage = new AddCommonSpacePage(firstBuildingId);
                MainFrame.Navigate(addCommonPage);
                await Task.Delay(PAUSE_BETWEEN_STEPS, token);

                // Run inline demo (not using StartDemo, to control pacing)
                addCommonPage.NameTextBox.IsReadOnly = true;
                addCommonPage.DescriptionTextBox.IsReadOnly = true;
                addCommonPage.FloorNumberTextBox.IsReadOnly = true;

                await TypeTextAsync(addCommonPage.NameTextBox, "Party Room", token);
                await Task.Delay(PAUSE_BETWEEN_STEPS, token);

                await TypeTextAsync(addCommonPage.DescriptionTextBox, "A room for birthday parties and gatherings.", token);
                await Task.Delay(PAUSE_BETWEEN_STEPS, token);

                await TypeTextAsync(addCommonPage.FloorNumberTextBox, "0", token);
                await Task.Delay(PAUSE_BETWEEN_STEPS, token);

                addCommonPage.OneDayRadioButton.IsChecked = true;
                await Task.Delay(PAUSE_BETWEEN_STEPS, token);

                // Highlight "Finish Adding" button
                var finishAddBtn = FindButtonByContent(addCommonPage, "Finish Adding");
                if (finishAddBtn != null)
                {
                    await SimulateButtonClick(finishAddBtn, token);
                }

                // =====================================================
                // Back to MyBuildings → +Assembly
                // =====================================================
                MainFrame.Navigate(new ManagerHomePage());
                await Task.Delay(1500, token);

                homePage = _currentPage as ManagerHomePage;
                if (homePage != null)
                {
                    var assemblyBtn = FindButtonByContent(homePage, "+ Assembly");
                    if (assemblyBtn != null)
                    {
                        await SimulateButtonClick(assemblyBtn, token);
                    }
                }

                // =====================================================
                // Schedule Assembly demo
                // =====================================================
                var schedulePage = new ScheduleAssemblyPage(firstBuildingId);
                MainFrame.Navigate(schedulePage);
                await Task.Delay(PAUSE_BETWEEN_STEPS, token);

                // Wait for Loaded event to populate buildings
                await Task.Delay(500, token);

                // Type time
                schedulePage.TbTime.IsReadOnly = true;
                schedulePage.DpDate.IsEnabled = false;
                schedulePage.BtnAddTopic.IsEnabled = false;
                schedulePage.TbNewTopic.IsReadOnly = true;

                // Set date
                schedulePage.DpDate.IsEnabled = true;
                schedulePage.DpDate.SelectedDate = DateTime.Today.AddDays(14);
                schedulePage.DpDate.IsEnabled = false;
                await Task.Delay(PAUSE_BETWEEN_STEPS, token);

                // Type time
                await TypeTextAsync(schedulePage.TbTime, "18:00", token);
                await Task.Delay(PAUSE_BETWEEN_STEPS, token);

                // Add topics
                var demoTopics = new[] { "Yearly maintenance plan", "Building renovation budget", "Parking lot rules" };
                foreach (var topic in demoTopics)
                {
                    schedulePage.TbNewTopic.IsReadOnly = false;
                    schedulePage.TbNewTopic.Text = "";
                    schedulePage.TbNewTopic.Foreground = Brushes.Black;

                    foreach (char c in topic)
                    {
                        token.ThrowIfCancellationRequested();
                        schedulePage.TbNewTopic.Text += c;
                        await Task.Delay(60, token);
                    }
                    schedulePage.TbNewTopic.IsReadOnly = true;
                    await Task.Delay(400, token);

                    // Simulate clicking Add button
                    schedulePage.BtnAddTopic.IsEnabled = true;
                    await SimulateButtonClickDirect(schedulePage.BtnAddTopic, token);
                    schedulePage.BtnAddTopic.IsEnabled = false;
                    await Task.Delay(400, token);
                }

                await Task.Delay(PAUSE_BETWEEN_STEPS, token);

                // Highlight "Schedule" button
                await SimulateButtonClick(schedulePage.BtnScheduleNow, token);
            }

            // Pause before cycling
            await Task.Delay(2000, token);
          } // end while
        }
        catch (TaskCanceledException) { }
        catch (OperationCanceledException) { }
        finally
        {
            HideDemoStopButton();
            _isDemoRunning = false;
            _demoCts?.Dispose();
            _demoCts = null;

            while (MainFrame.CanGoBack)
                MainFrame.RemoveBackEntry();
            MainFrame.Navigate(new ManagerHomePage());
        }
    }

    // ---------------------------------------------------------------
    // Demo helpers
    // ---------------------------------------------------------------

    /// <summary>Simulates a button hover (highlight) + 1s pause. Does NOT actually click.</summary>
    private static async Task SimulateButtonClick(Button button, CancellationToken token)
    {
        var origBg = button.Background;
        var origFg = button.Foreground;
        button.Background = HighlightBg;
        await Task.Delay(PAUSE_AFTER_HIGHLIGHT, token);
        button.Background = origBg;
        button.Foreground = origFg;
    }

    /// <summary>Simulates button highlight, then actually invokes the click handler.</summary>
    private static async Task SimulateButtonClickDirect(Button button, CancellationToken token)
    {
        var origBg = button.Background;
        button.Background = HighlightBg;
        await Task.Delay(300, token);
        button.Background = origBg;
        button.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent));
    }

    private static async Task TypeTextAsync(TextBox textBox, string text, CancellationToken token)
    {
        textBox.Text = "";
        foreach (char c in text)
        {
            token.ThrowIfCancellationRequested();
            textBox.Text += c;
            await Task.Delay(TYPING_DELAY, token);
        }
    }

    /// <summary>Checks if a DependencyObject is the target or a child of it.</summary>
    private static bool IsChildOf(DependencyObject child, DependencyObject parent)
    {
        var current = child;
        while (current != null)
        {
            if (current == parent) return true;
            current = VisualTreeHelper.GetParent(current);
        }
        return false;
    }

    /// <summary>Finds first Button with matching Content text in a page's visual tree.</summary>
    private static Button? FindButtonByContent(DependencyObject parent, string content)
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is Button btn && btn.Content is string s && s == content)
                return btn;
            var result = FindButtonByContent(child, content);
            if (result != null) return result;
        }
        return null;
    }

    // ---------------------------------------------------------------
    // Header / navigation
    // ---------------------------------------------------------------
    private void BackButton_Click(object sender, MouseButtonEventArgs e)
    {
        if (_isDemoRunning) return;
        if (MainFrame.CanGoBack) MainFrame.GoBack();
    }

    private void CloseMenu_Click(object sender, RoutedEventArgs e)
    {
        SideMenuOverlay.Visibility = Visibility.Collapsed;
    }

    private void MainFrame_Navigated(object sender, NavigationEventArgs e)
    {
        if (e.Content is Page page)
        {
            _currentPage = page;
            TxtTitle.Text = page.Title;

            // Don't override header button during demo — keep red STOP visible
            if (!_isDemoRunning)
            {
                bool showDemoAction = page is CreateBuildingPage ||
                                      page is AddCommonSpacePage ||
                                      page is RejectBuildingRequestPage ||
                                      page is ScheduleAssemblyPage;
                BtnHeaderAction.Content = showDemoAction ? "► DEMO" : "☰";
                BtnHeaderAction.FontSize = showDemoAction ? 12 : 18;
                BtnHeaderAction.Foreground = showDemoAction ? Brushes.Black : Brushes.White;
                BtnHeaderAction.Background = showDemoAction ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFD700")) : Brushes.Transparent;
                BtnHeaderAction.BorderBrush = showDemoAction ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFD700")) : Brushes.Transparent;
                BtnHeaderAction.BorderThickness = showDemoAction ? new Thickness(1) : new Thickness(0);
            }

            HeaderBorder.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#333333"));
            HeaderBorder.BorderBrush = Brushes.Transparent;
            HeaderBorder.BorderThickness = new Thickness(0);

            TxtTitle.Foreground = Brushes.White;
            TxtTitle.FontSize = 16;
            BtnBack.Foreground = Brushes.White;
            BtnBack.Visibility = MainFrame.CanGoBack ? Visibility.Visible : Visibility.Collapsed;
            BtnHeaderAction.Visibility = Visibility.Visible;
        }
    }

    // ---------------------------------------------------------------
    // Bottom nav / side nav
    // ---------------------------------------------------------------
    private void BottomNav_BuildingsClick(object sender, RoutedEventArgs e)
    {
        if (_isDemoRunning) return;
        SideMenuOverlay.Visibility = Visibility.Collapsed;
        MainFrame.Navigate(new ManagerHomePage());
    }

    private void BottomNav_MembershipsClick(object sender, RoutedEventArgs e)
    {
        if (_isDemoRunning) return;
        SideMenuOverlay.Visibility = Visibility.Collapsed;
        MainFrame.Navigate(new ManageBuildingRequestsPage());
    }

    private void SideNav_BuildingsClick(object sender, RoutedEventArgs e)
    {
        if (_isDemoRunning) return;
        SideMenuOverlay.Visibility = Visibility.Collapsed;
        MainFrame.Navigate(new ManagerHomePage());
    }

    private void SideNav_RegisterBuildingClick(object sender, RoutedEventArgs e)
    {
        if (_isDemoRunning) return;
        SideMenuOverlay.Visibility = Visibility.Collapsed;
        var createBuildingPage = new CreateBuildingPage();
        createBuildingPage.OnBuildingCreated += () => { MainFrame.Navigate(new ManagerHomePage()); };
        MainFrame.Navigate(createBuildingPage);
    }

    private void SideNav_ManageRequestsClick(object sender, RoutedEventArgs e)
    {
        if (_isDemoRunning) return;
        SideMenuOverlay.Visibility = Visibility.Collapsed;
        MainFrame.Navigate(new ManageBuildingRequestsPage());
    }

    private void SideNav_RentalRequestsClick(object sender, RoutedEventArgs e)
    {
        if (_isDemoRunning) return;
        SideMenuOverlay.Visibility = Visibility.Collapsed;
        MainFrame.Navigate(new ManagerViews.RentalRequests.RentalRequestsPage());
    }

    private void BottomNav_AssembliesClick(object sender, RoutedEventArgs e)
    {
        if (_isDemoRunning) return;
        SideMenuOverlay.Visibility = Visibility.Collapsed;
        MainFrame.Navigate(new AssembliesPage());
    }

    private void SideNav_AssembliesClick(object sender, RoutedEventArgs e)
    {
        if (_isDemoRunning) return;
        SideMenuOverlay.Visibility = Visibility.Collapsed;
        MainFrame.Navigate(new AssembliesPage());
    }

    private void SideNav_UnresolvedProblemsClick(object sender, RoutedEventArgs e)
    {
        if (_isDemoRunning) return;
        SideMenuOverlay.Visibility = Visibility.Collapsed;
        MainFrame.Navigate(new UnresolvedProblemsPage());
    }

    private void BottomNav_LogoutClick(object sender, RoutedEventArgs e)
    {
        if (_isDemoRunning) return;
        UserSession.SignOut();
        RedirectToLogin();
    }

    private void RedirectToLogin()
    {
        LogInForm logInForm = new();
        logInForm.Show();
        this.Close();
    }

    // ---------------------------------------------------------------
    // Toast notification helpers — called from child pages
    // ---------------------------------------------------------------

    /// <summary>Show green success toast. Optionally navigate back.</summary>
    public void ShowSuccessToast(string message, bool goBack = false)
    {
        Toast.ShowSuccess(message);
        if (goBack && MainFrame.CanGoBack)
            MainFrame.GoBack();
    }

    /// <summary>Show red error toast (stays until user dismisses).</summary>
    public void ShowErrorToast(string message)
    {
        Toast.ShowError(message);
    }

    /// <summary>Get the ManagerMainWindow from any child page.</summary>
    public static ManagerMainWindow? GetFrom(Page page)
    {
        return Window.GetWindow(page) as ManagerMainWindow;
    }
}
