using Ultimate_Gshop.Itens;

namespace Ultimate_Gshop.Format.Tests;

public sealed class ItemSelectionTests
{
    private static List<Item> Visible() => new()
    {
        new() { ID = 42, Place = 9 }, new() { ID = 42, Place = 2 },
        new() { ID = 42, Place = 7 }, new() { ID = 42, Place = 4 }
    };

    private static void AssertReferences(ItemSelection selection, params Item[] expected)
    {
        Assert.Equal(expected.Length, selection.Items.Count);
        for (var index = 0; index < expected.Length; index++) Assert.Same(expected[index], selection.Items[index]);
    }

    [Fact]
    public void PlainClickAndCtrlToggleUseRecordReferencesRatherThanDuplicateIds()
    {
        var visible = Visible();
        var selection = new ItemSelection();
        selection.Select(visible, visible[1], false, false);
        AssertReferences(selection, visible[1]);
        selection.Select(visible, visible[2], false, true);
        AssertReferences(selection, visible[1], visible[2]);
        selection.Select(visible, visible[1], false, true);
        AssertReferences(selection, visible[2]);
        selection.Select(visible, visible[0], false, false);
        AssertReferences(selection, visible[0]);
    }

    [Theory]
    [InlineData(0, 3)]
    [InlineData(3, 0)]
    [InlineData(1, 1)]
    public void ShiftSelectsTheDisplayedRangeInEitherDirection(int anchor, int target)
    {
        var visible = Visible();
        var selection = new ItemSelection();
        selection.Select(visible, visible[anchor], false, false);
        selection.Select(visible, visible[target], true, false);
        AssertReferences(selection, visible.Skip(Math.Min(anchor, target)).Take(Math.Abs(target - anchor) + 1).ToArray());
    }

    [Fact]
    public void RepeatedShiftKeepsTheOriginalAnchorAndCanShrinkTheRange()
    {
        var visible = Visible();
        var selection = new ItemSelection();
        selection.Select(visible, visible[0], false, false);
        selection.Select(visible, visible[3], true, false);
        selection.Select(visible, visible[1], true, false);
        AssertReferences(selection, visible[0], visible[1]);
    }

    [Fact]
    public void CtrlShiftAddsARangeWithoutDuplicatingPreviouslySelectedRecords()
    {
        var visible = Visible();
        var selection = new ItemSelection();
        selection.Select(visible, visible[0], false, false);
        selection.Select(visible, visible[2], false, true);
        selection.Select(visible, visible[3], true, true);
        selection.Select(visible, visible[3], true, true);
        AssertReferences(selection, visible[0], visible[2], visible[3]);
    }

    [Fact]
    public void RefreshClearsSelectionAndAnchorBeforeANewShiftClick()
    {
        var visible = Visible();
        var selection = new ItemSelection();
        selection.Select(visible, visible[0], false, false);
        selection.Reset();
        Assert.Empty(selection.Items);
        selection.Select(visible, visible[3], true, false);
        AssertReferences(selection, visible[3]);
    }

    [Fact]
    public void HiddenOrStaleReferencesCannotEnterOrRemainInSelection()
    {
        var visible = Visible();
        var selection = new ItemSelection();
        selection.Select(visible, visible[0], false, false);
        selection.Select(visible, visible[1].Clone(), false, true);
        AssertReferences(selection, visible[0]);
        selection.Select(visible.Skip(1).ToList(), visible[2], true, false);
        AssertReferences(selection, visible[2]);
        selection.Select([], visible[2], false, false);
        AssertReferences(selection, visible[2]);
    }
}
