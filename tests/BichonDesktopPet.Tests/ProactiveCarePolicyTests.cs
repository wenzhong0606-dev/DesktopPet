using System;
using BichonDesktopPet.Core.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BichonDesktopPet.Tests;

[TestClass]
public sealed class ProactiveCarePolicyTests
{
    private static CareContext Context => new(new DateTimeOffset(2026,11,28,12,0,0,TimeSpan.Zero),false,true,false,false,false,false,false);

    [TestMethod]
    public void SuppressionAppliesAcrossWeatherAndActivity()
    {
        var policy=new ProactiveCarePolicy();
        foreach(var type in Enum.GetValues<CareReminderType>())
        {
            Assert.IsTrue(policy.CanDeliver(type,Context));
            Assert.IsFalse(policy.CanDeliver(type,Context with { ChatVisible=true }));
            Assert.IsFalse(policy.CanDeliver(type,Context with { Active=false }));
            Assert.IsFalse(policy.CanDeliver(type,Context with { DoNotDisturb=true }));
            Assert.IsFalse(policy.CanDeliver(type,Context with { Busy=true }));
        }
        Assert.IsFalse(policy.CanDeliver(CareReminderType.TakeBreak,Context with { Active=null }));
        Assert.IsTrue(policy.CanDeliver(CareReminderType.ColdWeather,Context with { Active=null }));
        Assert.IsFalse(policy.CanDeliver(CareReminderType.TakeBreak,Context with { ReduceBreaks=true }));
    }

    [TestMethod]
    public void SharedCooldownAndRollingBudget()
    {
        var policy=new ProactiveCarePolicy();
        var start=Context.Now;
        for(var i=0;i<4;i++)
        {
            var now=start.AddHours(i*2);
            Assert.IsTrue(policy.CanDeliver(CareReminderType.TakeBreak,Context with { Now=now }));
            policy.MarkDelivered(now);
            Assert.IsFalse(policy.CanDeliver(CareReminderType.RainExpected,Context with { Now=now.AddMinutes(30) }));
        }
        Assert.IsFalse(policy.CanDeliver(CareReminderType.ColdWeather,Context with { Now=start.AddHours(10) }));
        Assert.IsTrue(policy.CanDeliver(CareReminderType.ColdWeather,Context with { Now=start.AddHours(24) }));
    }

    [TestMethod]
    public void RestartRestoresSharedBudget()
    {
        var first=new ProactiveCarePolicy();
        for(var i=0;i<4;i++) first.MarkDelivered(Context.Now.AddHours(i*2));
        var restarted=new ProactiveCarePolicy();
        restarted.Restore(first.Snapshot(),Context.Now.AddHours(8));
        Assert.IsFalse(restarted.CanDeliver(CareReminderType.RainExpected,Context with { Now=Context.Now.AddHours(8) }));
        Assert.IsTrue(restarted.CanDeliver(CareReminderType.TakeBreak,Context with { Now=Context.Now.AddHours(24) }));
    }

    [TestMethod]
    public void SuppressedActivityCandidateDoesNotConsumeCooldown()
    {
        var engine=new ActivityRhythmEngine();
        engine.Observe(Context.Now,TimeSpan.Zero,false);
        var now=Context.Now.AddHours(1);
        Assert.IsNotNull(engine.Observe(now,TimeSpan.Zero,false));
        Assert.IsNotNull(engine.Observe(now.AddSeconds(30),TimeSpan.Zero,false));
        engine.MarkDelivered(now.AddSeconds(30));
        Assert.IsNull(engine.Observe(now.AddMinutes(1),TimeSpan.Zero,false));
    }

    [TestMethod]
    public void ReducedFrequencyUsesLongerGapAndLowerBudget()
    {
        var policy=new ProactiveCarePolicy();
        var context=Context with { ReduceFrequency=true };
        policy.MarkDelivered(context.Now);
        Assert.IsFalse(policy.CanDeliver(CareReminderType.ColdWeather,context with { Now=context.Now.AddHours(2) }));
        Assert.IsTrue(policy.CanDeliver(CareReminderType.ColdWeather,context with { Now=context.Now.AddHours(3) }));
        policy.MarkDelivered(context.Now.AddHours(3));
        Assert.IsFalse(policy.CanDeliver(CareReminderType.RainExpected,context with { Now=context.Now.AddHours(8) }));
    }
}
