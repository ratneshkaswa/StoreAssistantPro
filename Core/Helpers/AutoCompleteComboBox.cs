using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace StoreAssistantPro.Core.Helpers;

/// <summary>
/// Adds Win11-style editable auto-suggest behavior to ComboBox by filtering
/// the control's own item view as the user types into the template text box.
/// </summary>
public static class AutoCompleteComboBox
{
    private static readonly ConditionalWeakTable<ComboBox, AutoCompleteSession> Sessions = [];

    public static readonly DependencyProperty IsEnabledProperty =
        DependencyProperty.RegisterAttached(
            "IsEnabled",
            typeof(bool),
            typeof(AutoCompleteComboBox),
            new PropertyMetadata(false, OnIsEnabledChanged));

    public static bool GetIsEnabled(DependencyObject obj) => (bool)obj.GetValue(IsEnabledProperty);

    public static void SetIsEnabled(DependencyObject obj, bool value) => obj.SetValue(IsEnabledProperty, value);

    private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not ComboBox comboBox)
        {
            return;
        }

        if ((bool)e.NewValue)
        {
            var session = Sessions.GetValue(comboBox, static combo => new AutoCompleteSession(combo));
            session.Attach();
        }
        else if (Sessions.TryGetValue(comboBox, out var session))
        {
            session.Detach();
            Sessions.Remove(comboBox);
        }
    }

    private sealed class AutoCompleteSession(ComboBox comboBox)
    {
        private readonly ComboBox _comboBox = comboBox;
        private TextBox? _editableTextBox;
        private bool _isAttached;
        private bool _isUpdatingText;

        public void Attach()
        {
            if (_isAttached)
            {
                ApplySharedSettings();
                AttachEditableTextBox();
                return;
            }

            _comboBox.Loaded += OnLoaded;
            _comboBox.Unloaded += OnUnloaded;
            _comboBox.SelectionChanged += OnSelectionChanged;
            _comboBox.DropDownClosed += OnDropDownClosed;
            _comboBox.LostKeyboardFocus += OnLostKeyboardFocus;

            ApplySharedSettings();
            AttachEditableTextBox();
            _isAttached = true;
        }

        public void Detach()
        {
            if (!_isAttached)
            {
                return;
            }

            _comboBox.Loaded -= OnLoaded;
            _comboBox.Unloaded -= OnUnloaded;
            _comboBox.SelectionChanged -= OnSelectionChanged;
            _comboBox.DropDownClosed -= OnDropDownClosed;
            _comboBox.LostKeyboardFocus -= OnLostKeyboardFocus;
            DetachEditableTextBox();
            ClearFilter();
            _isAttached = false;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            ApplySharedSettings();
            AttachEditableTextBox();
            SyncEditableText();
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            ClearFilter();
            DetachEditableTextBox();
        }

        private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SyncEditableText();

            if (!_comboBox.IsDropDownOpen)
            {
                ClearFilter();
            }
        }

        private void OnDropDownClosed(object? sender, EventArgs e)
        {
            ClearFilter();
            SyncEditableText();
        }

        private void OnLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            if (!_comboBox.IsKeyboardFocusWithin)
            {
                ClearFilter();
            }
        }

        private void ApplySharedSettings()
        {
            _comboBox.IsEditable = true;
            _comboBox.IsTextSearchEnabled = false;
            _comboBox.StaysOpenOnEdit = true;
        }

        private void AttachEditableTextBox()
        {
            _comboBox.ApplyTemplate();
            var nextTextBox = _comboBox.Template?.FindName("PART_EditableTextBox", _comboBox) as TextBox;

            if (ReferenceEquals(nextTextBox, _editableTextBox))
            {
                return;
            }

            DetachEditableTextBox();
            _editableTextBox = nextTextBox;

            if (_editableTextBox is null)
            {
                return;
            }

            _editableTextBox.TextChanged += OnEditableTextChanged;
            _editableTextBox.PreviewKeyDown += OnEditableTextPreviewKeyDown;
        }

        private void DetachEditableTextBox()
        {
            if (_editableTextBox is null)
            {
                return;
            }

            _editableTextBox.TextChanged -= OnEditableTextChanged;
            _editableTextBox.PreviewKeyDown -= OnEditableTextPreviewKeyDown;
            _editableTextBox = null;
        }

        private void OnEditableTextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isUpdatingText)
            {
                return;
            }

            UpdateFilter(_editableTextBox?.Text);
        }

        private void OnEditableTextPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key is Key.Down or Key.Up)
            {
                _comboBox.IsDropDownOpen = true;
            }

            if (e.Key == Key.Escape)
            {
                ClearFilter();
                _comboBox.IsDropDownOpen = false;
            }
        }

        private void UpdateFilter(string? filterText)
        {
            var query = (filterText ?? string.Empty).Trim();

            if (string.IsNullOrEmpty(query))
            {
                ClearFilter();
                return;
            }

            _comboBox.Items.Filter = item => Matches(item, query);
            _comboBox.Items.Refresh();
            _comboBox.IsDropDownOpen = _comboBox.Items.Count > 0;
        }

        private void ClearFilter()
        {
            if (_comboBox.Items.Filter is null)
            {
                return;
            }

            _comboBox.Items.Filter = null;
            _comboBox.Items.Refresh();
        }

        private void SyncEditableText()
        {
            if (_editableTextBox is null)
            {
                return;
            }

            var nextText = ResolveDisplayText(_comboBox.SelectedItem);

            if (string.Equals(_editableTextBox.Text, nextText, StringComparison.CurrentCulture))
            {
                return;
            }

            _isUpdatingText = true;
            _editableTextBox.Text = nextText;
            _editableTextBox.CaretIndex = _editableTextBox.Text.Length;
            _isUpdatingText = false;
        }

        private bool Matches(object? item, string query)
        {
            var candidate = ResolveDisplayText(item);
            return !string.IsNullOrWhiteSpace(candidate)
                && candidate.Contains(query, StringComparison.CurrentCultureIgnoreCase);
        }

        private string ResolveDisplayText(object? item)
        {
            if (item is null)
            {
                return string.Empty;
            }

            if (item is string text)
            {
                return text;
            }

            if (item is DependencyObject dependencyObject)
            {
                var explicitText = TextSearch.GetText(dependencyObject);
                if (!string.IsNullOrWhiteSpace(explicitText))
                {
                    return explicitText;
                }
            }

            var textPath = TextSearch.GetTextPath(_comboBox);
            if (TryResolvePropertyPath(item, textPath, out var textPathValue))
            {
                return textPathValue;
            }

            if (TryResolvePropertyPath(item, _comboBox.DisplayMemberPath, out var displayMemberValue))
            {
                return displayMemberValue;
            }

            return Convert.ToString(item, CultureInfo.CurrentCulture) ?? string.Empty;
        }

        private static bool TryResolvePropertyPath(object item, string? propertyPath, out string value)
        {
            value = string.Empty;
            if (string.IsNullOrWhiteSpace(propertyPath))
            {
                return false;
            }

            object? current = item;
            foreach (var segment in propertyPath.Split('.'))
            {
                if (current is null)
                {
                    return false;
                }

                var property = current.GetType().GetProperty(segment);
                if (property is null)
                {
                    return false;
                }

                current = property.GetValue(current);
            }

            value = Convert.ToString(current, CultureInfo.CurrentCulture) ?? string.Empty;
            return !string.IsNullOrWhiteSpace(value);
        }
    }
}
