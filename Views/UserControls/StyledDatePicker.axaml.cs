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
        }

        public DateTime? SelectedDate
        {
            get => GetValue(SelectedDateProperty);
            set
            {
                SetValue(SelectedDateProperty, value);
                UpdateFormattedDate();
            }
        }

        public string LabelText
        {
            get => GetValue(LabelTextProperty);
            set => SetValue(LabelTextProperty, value);
        }

        private void UpdateLabelVisibility(string? newValue)
        {
            if (_label != null && !string.IsNullOrWhiteSpace(newValue))
            {
                _label.Text = newValue;
                _label.IsVisible = true;
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

        public int? DateAsInt =>
            SelectedDate is DateTime dt
                ? dt.Year * 10000 + dt.Month * 100 + dt.Day
                : null;
    }
}
