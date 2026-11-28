using System;
using System.IO;
using System.Linq;
using BichonDesktopPet.Core.AI;
using BichonDesktopPet.Core.Models;
using BichonDesktopPet.Core.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BichonDesktopPet.Tests;

[TestClass]
public sealed class PersonalityGrowthStoreTests
{
    private string _directory = null!, _path = null!;
    private CompanionIdentityStore _identities = null!;
    private CompanionIdentity _first = null!, _second = null!;
    private ChatHistoryStore _history = null!;
    private PersonalityGrowthStore _store = null!;
    private MutableClock _clock = null!;
    private string _session = null!;
    private const string Brief = "我希望你回答简洁一点";
    private const string Detailed = "我希望你回答详细一点";

    [TestInitialize]
    public void SetUp()
    {
        _directory = Path.Combine(Path.GetTempPath(), "DesktopPet.GrowthTests", Guid.NewGuid().ToString("N"));
        _path = Path.Combine(_directory, "companions.db");
        _identities = new CompanionIdentityStore(_path);
        _identities.Initialize();
        _first = _identities.EnsureCompanion(new AvatarProfile { Id = "a", Name = "A", Persona = new PersonaProfile { SpeechStyle = "cute" } });
        _second = _identities.EnsureCompanion(new AvatarProfile { Id = "b", Name = "B" });
        _history = new ChatHistoryStore(_path);
        _history.Initialize();
        _session = _history.CreateSession(_first.Id).Id;
        _clock = new MutableClock(new DateTimeOffset(2026,11,28,10,0,0,TimeSpan.Zero));
        _store = new PersonalityGrowthStore(_path,_clock);
        _store.Initialize();
    }

    [TestCleanup]
    public void CleanUp() => Directory.Delete(_directory,true);

    private long Add(string content, string role = "user") =>
        _history.SaveMessage(_first.Id,_session,role,content,_clock.Now).Id;

    private bool Observe(string content) => _store.Observe(_first.Id,Add(content));
    private void NextDay() => _clock.Now = _clock.Now.AddDays(1);

    private void ThreeDays(string text)
    {
        Assert.IsFalse(Observe(text));
        NextDay();
        Assert.IsFalse(Observe(text));
        NextDay();
        Assert.IsTrue(Observe(text));
    }

    [TestMethod]
    public void Observe_DefaultDisabledAndNoBackfillOrForeignAssistantEvidence()
    {
        var old = Add(Brief);
        Assert.IsFalse(_store.Read(_first.Id).Enabled);
        Assert.IsFalse(_store.Observe(_first.Id,old));
        _store.SetEnabled(_first.Id,true);
        _store.SetEnabled(_second.Id,true);
        Assert.IsFalse(_store.Observe(_first.Id,old));
        var user = Add(Brief);
        Assert.IsFalse(_store.Observe(_second.Id,user));
        Assert.IsFalse(_store.Observe(_first.Id,user));
        NextDay();
        Assert.IsFalse(_store.Observe(_first.Id,Add(Brief,"assistant")));
        Assert.IsFalse(Observe("假设我希望你回答简洁一点"));
        Assert.AreEqual(0,_store.Read(_first.Id).Length);
        Assert.AreEqual(0,_store.Read(_second.Id).Length);
    }

    [TestMethod]
    public void Observe_RequiresDistinctDaysAndPersistsWithoutMutatingBasePersona()
    {
        _store.SetEnabled(_first.Id,true);
        Assert.IsFalse(Observe(Brief));
        Assert.IsFalse(Observe(Brief));
        Assert.IsFalse(Observe(Brief));
        Assert.AreEqual(0,_store.Read(_first.Id).Length);
        NextDay();
        Assert.IsFalse(Observe(Brief));
        NextDay();
        Assert.IsTrue(Observe(Brief));
        var reopened = new PersonalityGrowthStore(_path,_clock);
        reopened.Initialize();
        Assert.AreEqual(-1,reopened.Read(_first.Id).Length);
        Assert.AreEqual(1,reopened.RecentChanges(_first.Id).Count);
        Assert.AreEqual(_first.CorePersonalityJson,_identities.GetByAvatarId("a")!.CorePersonalityJson);
        Assert.AreEqual(_first.EvolvingPersonalityJson,_identities.GetByAvatarId("a")!.EvolvingPersonalityJson);
    }

    [TestMethod]
    public void Observe_ChangesAreBoundedAndOppositeEvidenceReversesGradually()
    {
        _store.SetEnabled(_first.Id,true);
        ThreeDays(Brief);
        NextDay();
        ThreeDays(Brief);
        for(int i=0;i<3;i++) { NextDay(); Assert.IsFalse(Observe(Brief)); }
        Assert.AreEqual(-2,_store.Read(_first.Id).Length);
        NextDay();
        ThreeDays(Detailed);
        Assert.AreEqual(-1,_store.Read(_first.Id).Length);
        Assert.AreEqual(3,_store.RecentChanges(_first.Id).Count);
    }

    [TestMethod]
    public void Observe_ConflictingAndExpiredPendingEvidenceCannotAccumulate()
    {
        _store.SetEnabled(_first.Id,true);
        Assert.IsFalse(Observe(Brief));
        NextDay();
        Assert.IsFalse(Observe(Brief));
        Assert.IsFalse(Observe(Detailed));
        NextDay();
        Assert.IsFalse(Observe(Brief));
        _clock.Now=_clock.Now.AddDays(61);
        Assert.IsFalse(Observe(Brief));
        NextDay();
        Assert.IsFalse(Observe(Brief));
        Assert.AreEqual(0,_store.Read(_first.Id).Length);
    }

    [TestMethod]
    public void PauseRollbackAndResetPreserveIsolationAndPreventReplay()
    {
        _store.SetEnabled(_first.Id,true);
        ThreeDays(Brief);
        NextDay();
        ThreeDays("我希望你回答直接一点");
        var change=_store.RecentChanges(_first.Id).First();
        Assert.AreEqual(1,_store.Read(_first.Id).Directness);
        _store.SetEnabled(_first.Id,false);
        Assert.AreEqual("",_store.Read(_first.Id).PromptHint());
        var paused=Add(Brief);
        Assert.IsFalse(_store.Observe(_first.Id,paused));
        _store.SetEnabled(_first.Id,true);
        Assert.IsFalse(_store.Observe(_first.Id,paused));
        Assert.IsTrue(_store.RollbackLast(_first.Id));
        Assert.AreEqual(0,_store.Read(_first.Id).Directness);
        Assert.AreEqual(-1,_store.Read(_first.Id).Length);
        Assert.IsFalse(_store.Observe(_first.Id,change.SourceMessageId));
        Assert.IsFalse(_store.RollbackLast(_second.Id));
        _store.Reset(_first.Id);
        Assert.AreEqual(0,_store.Read(_first.Id).Length);
        Assert.AreEqual(0,_store.RecentChanges(_first.Id).Count);
        Assert.IsFalse(_store.Observe(_first.Id,change.SourceMessageId));
        Assert.IsTrue(_store.Read(_first.Id).Enabled);
    }

    [TestMethod]
    public void ContextAndCare_UseOnlyEnabledBoundedHintsAndPreserveBase()
    {
        var avatar=new AvatarProfile { Persona=new PersonaProfile { SpeechStyle="cute", Nicknames=["小白"], UserAddress="伙伴" } };
        var active=new PersonalityGrowthProfile(true,-1,1,1,1);
        var paused=active with { Enabled=false };
        var plain=ChatContextBuilder.BuildMessages(_first,avatar,PetBehaviorState.Idle,[]);
        var adapted=ChatContextBuilder.BuildMessages(_first,avatar,PetBehaviorState.Idle,[],growth:active);
        var disabled=ChatContextBuilder.BuildMessages(_first,avatar,PetBehaviorState.Idle,[],growth:paused);
        StringAssert.Contains(adapted[0].Content,"【渐进表达倾向】");
        StringAssert.Contains(adapted[0].Content,"事实准确性及任务完整性优先");
        StringAssert.Contains(adapted[0].Content,"小白");
        StringAssert.Contains(adapted[0].Content,"可爱、亲切");
        Assert.IsFalse(disabled[0].Content.Contains("【渐进表达倾向】"));
        Assert.IsFalse(plain[0].Content.Contains("【渐进表达倾向】"));
        var normalCare=CareReminderMessageBuilder.BuildBreakReminder(avatar);
        Assert.AreEqual(normalCare,CareReminderMessageBuilder.BuildBreakReminder(avatar,paused));
        var learnedCare=CareReminderMessageBuilder.BuildBreakReminder(avatar,active);
        Assert.AreNotEqual(normalCare,learnedCare);
        StringAssert.Contains(learnedCare,"伙伴");
        StringAssert.Contains(learnedCare,"一小时");
        StringAssert.Contains(learnedCare,"休息眼睛");
        Assert.AreEqual("cute",avatar.Persona.SpeechStyle);
    }

    private sealed class MutableClock(DateTimeOffset now) : TimeProvider
    {
        public DateTimeOffset Now { get; set; }=now;
        public override DateTimeOffset GetUtcNow()=>Now;
    }
}
