using System.Windows;
using System.Windows.Controls;
using CommunityHub.Application;
using CommunityHub.Application.Domain;
using CommunityHub.Application.Services;

namespace CommunityHub.Ui.Views.CoordinatorViews;

public partial class BudgetWindow : Window
{
    private readonly long _userId;
    private readonly BudgetService _budgetService;
    private readonly NeighborhoodService _neighborhoodService;
    private List<Neighborhood> _neighborhoods;
    private List<BudgetCategory> _categories;
    private BudgetCategory? _selectedCategory;

    public BudgetWindow(long userId)
    {
        InitializeComponent();
        _userId = userId;
        _budgetService = Injector.CreateInstance<BudgetService>();
        _neighborhoodService = Injector.CreateInstance<NeighborhoodService>();
        _neighborhoods = new List<Neighborhood>();
        _categories = new List<BudgetCategory>();

        LoadNeighborhoods();
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
        LoadCategories(selected.Id);
    }

    private void LoadCategories(long neighborhoodId)
    {
        _categories = _budgetService.GetCategories(neighborhoodId);
        CategoriesListBox.ItemsSource = _categories.Select(c =>
            $"{c.Name} — {c.TotalAmount:F2} RSD").ToList();
    }

    private void CategoriesListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        int index = CategoriesListBox.SelectedIndex;
        if (index < 0 || index >= _categories.Count) return;

        _selectedCategory = _categories[index];

        var donations = _budgetService.GetDonations(_selectedCategory.Id);
        DonationsDataGrid.ItemsSource = donations.Select(d => new
        {
            Amount = $"{d.Amount:F2} RSD",
            DonatedAt = d.DonatedAt.ToString("dd.MM.yyyy")
        }).ToList();

        var expenses = _budgetService.GetExpenses(_selectedCategory.Id);
        ExpensesDataGrid.ItemsSource = expenses.Select(ex => new
        {
            Amount = $"{ex.Amount:F2} RSD",
            Description = ex.Description,
            ExpenseDate = ex.ExpenseDate.ToString()
        }).ToList();
    }

    private void AddExpenseButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (_selectedCategory == null)
            {
                MessageBox.Show("Odaberite kategoriju.", "Greška",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!decimal.TryParse(ExpenseAmountTextBox.Text, out decimal amount) || amount <= 0)
            {
                MessageBox.Show("Unesite ispravan iznos.", "Greška",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(ExpenseDescriptionTextBox.Text))
            {
                MessageBox.Show("Unesite opis troška.", "Greška",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (ExpenseDatePicker.SelectedDate == null)
            {
                MessageBox.Show("Odaberite datum troška.", "Greška",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            DateOnly expenseDate = DateOnly.FromDateTime(ExpenseDatePicker.SelectedDate.Value);

            _budgetService.AddExpense(_selectedCategory.Id, amount,
                ExpenseDescriptionTextBox.Text, expenseDate);

            MessageBox.Show("Trošak uspešno dodat.", "Uspeh",
                MessageBoxButton.OK, MessageBoxImage.Information);

            ExpenseAmountTextBox.Clear();
            ExpenseDescriptionTextBox.Clear();
            ExpenseDatePicker.SelectedDate = null;

            if (NeighborhoodComboBox.SelectedItem is Neighborhood selected)
                LoadCategories(selected.Id);

            CategoriesListBox_SelectionChanged(sender, null!);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Greška",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BackButton_Click(object sender, RoutedEventArgs e)
    {
        HomeWindow homeWindow = new HomeWindow(_userId);
        homeWindow.Show();
        this.Close();
    }
}