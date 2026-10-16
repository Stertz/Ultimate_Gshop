using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Ultimate_Gshop.Views.UserControls
{
    public partial class StyledDatePicker : UserControl
    {
        public static readonly StyledProperty<DateTime?> SelectedDateProperty =
            AvaloniaProperty.Register<StyledDatePicker, DateTime?>(nameof(SelectedDate));

        public static readonly StyledProperty<string> LabelTextProperty =
            AvaloniaProperty.Register<StyledDatePicker, string>(nameof(LabelText));

        private readonly TextBlock? _label;
        private readonly DatePicker? _picker;
        private readonly TextBlock? _formattedText;

        public StyledDatePicker()
        {
            AvaloniaXamlLoader.Load(this);

            _label = this.FindControl<TextBlock>("Label");
            _picker = this.FindControl<DatePicker>("Picker");
            _formattedText = this.FindControl<TextBlock>("FormattedDateText");

            if (_picker != null)
                _picker.SelectedDateChanged += OnSelectedDateChanged;

            this.GetObservable(LabelTextProperty).Subscribe(UpdateLabelVisibility);
            this.GetObservable(SelectedDateProperty).Subscribe(_ => SynchronizeDate());
        }

        public DateTime? SelectedDate
        {
            get => GetValue(SelectedDateProperty);
            set
            {
                SetValue(SelectedDateProperty, value);
            }
        }

        public string LabelText
        {
            get => GetValue(LabelTextProperty);
            set => SetValue(LabelTextProperty, value);
        }

        private void UpdateLabelVisibility(string? newValue)
        {
            if (_label != null)
            {
                _label.Text = newValue ?? string.Empty;
                _label.IsVisible = !string.IsNullOrWhiteSpace(newValue);
            }
        }

        private void OnSelectedDateChanged(object? sender, DatePickerSelectedValueChangedEventArgs e)
        {
            SelectedDate = e.NewDate?.Date;
        }

        private void UpdateFormattedDate()
        {
            if (_formattedText != null)
            {
                _formattedText.Text = SelectedDate.HasValue
                    ? SelectedDate.Value.ToString("dd/MM/yy")
                    : "";
            }
        }

        private void SynchronizeDate()
        {
            if (_picker != null && _picker.SelectedDate?.Date != SelectedDate?.Date)
                _picker.SelectedDate = SelectedDate is DateTime date
                    ? new DateTimeOffset(DateTime.SpecifyKind(date.Date, DateTimeKind.Unspecified), TimeSpan.Zero)
                    : null;
            UpdateFormattedDate();
        }

        public int? DateAsInt =>
            SelectedDate is DateTime dt
                ? dt.Year * 10000 + dt.Month * 100 + dt.Day
                : null;
    }
}
