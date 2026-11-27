using System;
using BichonDesktopPet.Core.Models;
using BichonDesktopPet.Core.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BichonDesktopPet.Tests;

[TestClass]
public sealed class PetBehaviorEngineTests
{
    private static readonly DateTimeOffset StartTime =
        new(2026, 11, 27, 9, 0, 0, TimeSpan.Zero);

    [TestMethod]
    public void Start_EntersGreetingThenReturnsToIdle()
    {
        var engine = new PetBehaviorEngine();

        Assert.AreEqual(PetBehaviorState.Greeting, engine.Start(StartTime));
        Assert.AreEqual(PetBehaviorState.Greeting, engine.Advance(StartTime.AddSeconds(2)));

        Assert.AreEqual(PetBehaviorState.Idle, engine.Advance(StartTime.AddSeconds(3)));
    }

    [TestMethod]
    public void PetClick_EntersHappyAndExpiresToIdle()
    {
        var engine = new PetBehaviorEngine();
        engine.Start(StartTime);

        Assert.AreEqual(PetBehaviorState.Happy, engine.OnPetClicked(StartTime.AddSeconds(1)));
        Assert.AreEqual(PetBehaviorState.Happy, engine.Advance(StartTime.AddSeconds(2)));

        Assert.AreEqual(PetBehaviorState.Idle, engine.Advance(StartTime.AddSeconds(3)));
    }

    [TestMethod]
    public void ThreeRapidClicks_EnterExcited()
    {
        var engine = new PetBehaviorEngine();
        engine.Start(StartTime);

        engine.OnPetClicked(StartTime.AddSeconds(1));
        engine.OnPetClicked(StartTime.AddSeconds(2));

        Assert.AreEqual(PetBehaviorState.Excited, engine.OnPetClicked(StartTime.AddSeconds(3)));
    }

    [TestMethod]
    public void ClicksOutsideRapidWindow_StartANewInteractionBurst()
    {
        var engine = new PetBehaviorEngine();
        engine.Start(StartTime);

        engine.OnPetClicked(StartTime.AddSeconds(1));
        engine.OnPetClicked(StartTime.AddSeconds(2));

        Assert.AreEqual(PetBehaviorState.Happy, engine.OnPetClicked(StartTime.AddSeconds(8)));
    }

    [TestMethod]
    public void GiftOpened_EntersExcited()
    {
        var engine = new PetBehaviorEngine();
        engine.Start(StartTime);

        Assert.AreEqual(PetBehaviorState.Excited, engine.OnGiftOpened(StartTime.AddSeconds(1)));
    }

    [TestMethod]
    public void Inactivity_EntersSleepyAndRemainsThereUntilInteraction()
    {
        var engine = new PetBehaviorEngine();
        engine.Start(StartTime);

        Assert.AreEqual(
            PetBehaviorState.Sleepy,
            engine.Advance(StartTime.Add(PetBehaviorEngine.SleepAfterInactivity)));

        Assert.AreEqual(PetBehaviorState.Sleepy, engine.Advance(StartTime.AddMinutes(2)));
        Assert.AreEqual(PetBehaviorState.Happy, engine.OnPetClicked(StartTime.AddMinutes(2).AddSeconds(1)));
    }

    [TestMethod]
    public void Wake_EntersGreetingAndResetsInactivityClock()
    {
        var engine = new PetBehaviorEngine();
        engine.Start(StartTime);
        engine.Advance(StartTime.Add(PetBehaviorEngine.SleepAfterInactivity));

        Assert.AreEqual(PetBehaviorState.Greeting, engine.OnWake(StartTime.AddSeconds(61)));
        Assert.AreEqual(PetBehaviorState.Idle, engine.Advance(StartTime.AddSeconds(64)));
        Assert.AreEqual(PetBehaviorState.Idle, engine.Advance(StartTime.AddSeconds(120)));
        Assert.AreEqual(PetBehaviorState.Sleepy, engine.Advance(StartTime.AddSeconds(122)));
    }

    [TestMethod]
    public void AdvanceBeforeStart_StartsWithGreeting()
    {
        var engine = new PetBehaviorEngine();

        Assert.AreEqual(PetBehaviorState.Greeting, engine.Advance(StartTime));
    }
}
