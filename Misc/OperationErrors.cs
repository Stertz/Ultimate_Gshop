using Avalonia.Controls;
using System.Text.Json;
using Ultimate_Gshop.Views;

namespace Ultimate_Gshop.Misc;

internal static class OperationErrors
{
    internal static bool IsRecoverable(Exception error) => error is
        IOException or UnauthorizedAccessException or JsonException or
        InvalidOperationException or ArgumentException or OverflowException or NotSupportedException;

    internal static Task<string> ShowAsync(Window owner, string title, Exception error) =>
        new CustomMessageBox(title, error.Message, CustomMessageBox.MessageBoxIcon.Error, "OK").ShowDialogAsync(owner);
}
