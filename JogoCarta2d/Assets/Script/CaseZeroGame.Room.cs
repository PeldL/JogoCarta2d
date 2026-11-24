using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;
using Pages = CaseZeroPages;

// The interrogation room: the Interviews page picks a person and "Interview" brings them here.
// The room (scene SalaInterrogatorio, component CaseZeroInterrogationRoom) offers questioning,
// the Rorschach test, or leaving. The dialogue itself is still the case data in CaseZeroCase.interviews.
public sealed partial class CaseZeroGame
{
    const string RoomScene = "SalaInterrogatorio";
    CaseZeroInterrogationRoom room;
    bool roomBusy;

    bool RoomOpen => room != null && room.IsOpen;

    TMPro.TMP_FontAsset RoomFont =>
        view.interviewChoiceLabels != null && view.interviewChoiceLabels.Length > 0 && view.interviewChoiceLabels[0] != null
            ? view.interviewChoiceLabels[0].font
            : view.documentTitle != null ? view.documentTitle.font : null;

    // Called by the room component (Awake) when the SalaInterrogatorio scene loads.
    public void RegisterRoom(CaseZeroInterrogationRoom loaded)
    {
        room = loaded;
        room.Initialize(this, RoomFont);
    }

    bool CanUseRoom() => state != null && !state.Finished && !travelling && !presenting && !confirming && !roomBusy
        && !RoomOpen && phone.CurrentCall == null;

    /// <summary>Takes interviewee <paramref name="index"/> to the interrogation room.</summary>
    public void OpenInterrogationRoom(int index)
    {
        if (!CanUseRoom() || index < 0 || index >= CurrentCase.interviewNames.Length) return;
        if (!Application.CanStreamedLevelBeLoaded(RoomScene)) { Say("Add the SalaInterrogatorio scene to the build profile."); return; }
        StartCoroutine(EnterRoom(index));
    }

    IEnumerator EnterRoom(int index)
    {
        roomBusy = true;
        state.idleTime = 0;
        CaseZeroSoundFeedback.Current?.PlayTransition();
        yield return FadeTravelCurtain(1, 0.35f);
        CloseDocument();
        interrogation.EndInterrogation();
        state.interviewSuspect = index;
        state.interviewNode = "start";
        interviewResumed = false;
        if (index < caseSuspects.Count) suspectAsset = caseSuspects[index];
        room = null;
        yield return SceneManager.LoadSceneAsync(RoomScene, LoadSceneMode.Additive);
        if (room == null) // The room registers itself in Awake.
        {
            Debug.LogError("[Caso Zero] A cena SalaInterrogatorio precisa ter o componente CaseZeroInterrogationRoom.");
            var broken = SceneManager.GetSceneByName(RoomScene);
            if (broken.isLoaded) yield return SceneManager.UnloadSceneAsync(broken);
            yield return FadeTravelCurtain(0, 0.35f);
            roomBusy = false;
            yield break;
        }
        room.Open(CurrentCase.id, index, CurrentCase.interviewNames[index]);
        // The room draws above the game UI and starts covered by its own curtain.
        view.travelCurtain.alpha = 0;
        view.travelCurtain.blocksRaycasts = false;
        view.travelCurtain.gameObject.SetActive(false);
        yield return room.Reveal();
        roomBusy = false;
        Save(false);
    }

    public void LeaveInterrogationRoom()
    {
        if (!RoomOpen || roomBusy) return;
        StartCoroutine(ExitRoom());
    }

    IEnumerator ExitRoom()
    {
        roomBusy = true;
        yield return room.Cover();
        interrogation.EndInterrogation();
        state.interviewNode = "start";
        view.travelCurtain.gameObject.SetActive(true);
        view.travelCurtain.alpha = 1;
        view.travelCurtain.blocksRaycasts = true;
        room = null;
        var scene = SceneManager.GetSceneByName(RoomScene);
        if (scene.isLoaded) yield return SceneManager.UnloadSceneAsync(scene);
        // Back to the case file: a fresh statement goes straight to Evidence, otherwise Interviews.
        if (showTestimonyOnExit)
        {
            showTestimonyOnExit = false;
            SelectEvidence(Testimony);
        }
        else OpenPage(Pages.Interviews);
        yield return FadeTravelCurtain(0, 0.35f);
        roomBusy = false;
        Save(false);
    }

    IEnumerator FadeTravelCurtain(float target, float duration)
    {
        var curtain = view.travelCurtain;
        if (curtain == null) yield break;
        curtain.gameObject.SetActive(true);
        curtain.blocksRaycasts = true;
        yield return DOTween.To(() => curtain.alpha, a => curtain.alpha = a, target, duration).SetEase(Ease.InOutSine).SetUpdate(true).WaitForCompletion();
        if (target <= 0) { curtain.blocksRaycasts = false; curtain.gameObject.SetActive(false); }
    }

    /// <summary>"Interrogate" in the room: starts the conversation from its opening line.</summary>
    public void BeginRoomInterview()
    {
        if (state == null || !RoomOpen || roomBusy) return;
        state.interviewedMask |= 1 << state.interviewSuspect;
        state.idleTime = 0;
        interviewResumed = false;
        interrogation.StartInterrogation(suspectAsset); // InterviewUpdated renders the first node in the room.
        Save(false);
    }

    void RenderRoomInterview(InterrogationNode node)
    {
        if (node == null) return;
        var labels = new string[node.choices.Count];
        var available = new bool[node.choices.Count];
        for (int i = 0; i < node.choices.Count; i++)
        {
            available[i] = IsChoiceAvailable(node.choices[i]);
            labels[i] = available[i] ? node.choices[i].choiceText : LockedChoiceText(node.choices[i]);
        }
        string message = node.nodeId == "start" && interviewResumed ? "Anything else, detective?" : node.message;
        room.RenderInterview(message, labels, available);
    }

    /// <summary>Called by the room when the Rorschach drawing has been compared with the answers.</summary>
    public void RecordRorschach(int index, bool psychopath, string hint)
    {
        if (state == null) return;
        state.rorschachMask |= 1 << index;
        state.interviewedMask |= 1 << index;
        state.idleTime = 0;
        if (psychopath && !string.IsNullOrEmpty(hint)) Say(hint);
        Save(false);
    }
}
