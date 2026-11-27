using System;
using BichonDesktopPet.Core.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BichonDesktopPet.Tests;

[TestClass]
public sealed class PetClickGestureRecognizerTests
{
    private static readonly DateTimeOffset StartTime =
        new(2026, 11, 27, 8, 0, 0, TimeSpan.Zero);

    private static PetClickGestureRecognizer CreateRecognizer() =>
        new(TimeSpan.FromMilliseconds(500), doubleClickWidth: 8, doubleClickHeight: 8);

    [TestMethod]
    public void FirstClick_RemainsPendingUntilDoubleClickWindowExpires()
    {
        var recognizer = CreateRecognizer();

        var result = recognizer.RegisterClick(StartTime, 100, 100);

        Assert.AreEqual(0, result.CompletedSingleClicks);
        Assert.IsFalse(result.IsDoubleClick);
        Assert.IsTrue(result.HasPendingSingleClick);
        Assert.IsFalse(recognizer.TryCompleteSingleClick(StartTime.AddMilliseconds(499)));
        Assert.IsTrue(recognizer.TryCompleteSingleClick(StartTime.AddMilliseconds(500)));
        Assert.IsFalse(recognizer.FlushPendingSingleClick());
    }

    [TestMethod]
    public void SecondClickWithinTimeAndPosition_IsOneDoubleClickAndNoSingleClicks()
    {
        var recognizer = CreateRecognizer();
        recognizer.RegisterClick(StartTime, 100, 100);

        var result = recognizer.RegisterClick(StartTime.AddMilliseconds(250), 104, 103);

        Assert.AreEqual(0, result.CompletedSingleClicks);
        Assert.IsTrue(result.IsDoubleClick);
        Assert.IsFalse(result.HasPendingSingleClick);
        Assert.IsFalse(recognizer.FlushPendingSingleClick());
    }

    [TestMethod]
    public void TwoClicksOutsideTimeWindow_AreTwoSeparateSingleClicks()
    {
        var recognizer = CreateRecognizer();
        recognizer.RegisterClick(StartTime, 100, 100);

        Assert.IsTrue(recognizer.TryCompleteSingleClick(StartTime.AddMilliseconds(500)));

        var second = recognizer.RegisterClick(StartTime.AddMilliseconds(700), 100, 100);

        Assert.AreEqual(0, second.CompletedSingleClicks);
        Assert.IsFalse(second.IsDoubleClick);
        Assert.IsTrue(second.HasPendingSingleClick);
        Assert.IsTrue(recognizer.TryCompleteSingleClick(StartTime.AddMilliseconds(1200)));
    }

    [TestMethod]
    public void LateSecondClick_CompletesPreviousSingleAndStartsNewPendingClick()
    {
        var recognizer = CreateRecognizer();
        recognizer.RegisterClick(StartTime, 100, 100);

        var second = recognizer.RegisterClick(StartTime.AddMilliseconds(501), 100, 100);

        Assert.AreEqual(1, second.CompletedSingleClicks);
        Assert.IsFalse(second.IsDoubleClick);
        Assert.IsTrue(second.HasPendingSingleClick);
        Assert.IsTrue(recognizer.FlushPendingSingleClick());
    }

    [TestMethod]
    public void ClickOutsideDoubleClickRectangle_CompletesPreviousSingle()
    {
        var recognizer = CreateRecognizer();
        recognizer.RegisterClick(StartTime, 100, 100);

        var second = recognizer.RegisterClick(StartTime.AddMilliseconds(200), 120, 100);

        Assert.AreEqual(1, second.CompletedSingleClicks);
        Assert.IsFalse(second.IsDoubleClick);
        Assert.IsTrue(second.HasPendingSingleClick);
    }

    [TestMethod]
    public void FlushPendingSingleClick_PreservesClickBeforeHideOrExit()
    {
        var recognizer = CreateRecognizer();
        recognizer.RegisterClick(StartTime, 100, 100);

        Assert.IsTrue(recognizer.FlushPendingSingleClick());
        Assert.IsFalse(recognizer.FlushPendingSingleClick());
    }
}
