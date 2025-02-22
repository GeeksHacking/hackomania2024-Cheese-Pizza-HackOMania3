using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HuggingFace.API;
using TMPro;
using UnityEngine.UI;

using UnityEngine.Events;
using DG.Tweening;
using System.IO;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class Chatbot : MonoBehaviour
{

    [Header("Context and conditioning")]
    [TextArea(15,15)]
    public string Context, FailedToGenerateText;
    [TextArea(2, 3)]
    public string InputHeader, InputCloser, Seperator;

    [TextArea(5, 5)]
    public string GreetingsMessage;
    //I have a string var s that i want to get whatever is after the seperator var.

    [Header("Variables")]
    public TMP_InputField TextInputfield;

    [Header("Response Management")]
    public string currentResponse;
    public string temptext, ContextedText;
    public GameObject ResponsePrefab, YourMessagePrefab, ResponseChallengePrefab;
    public Transform SpeechBubblesParent;

    public List<GameObject> InstantiatedTextboxes = new();

    [Header("Adding Images to responses")]
    public GameObject ImageWithButtonprefab;
    public GameObject CustomButtonPrefab;
    public Sprite SummarizeArticleIconSprite;

    [Header("ModularChatbotFuncs")]
    public Chatbot_ST CBM_SentenceTransform;
    public Chatbot_AT CBM_AutoTokenizer;
    public Chatbot_SU CBM_Summarizer;

    public Button SendButton;

    [Header("ImplementationsToUI")]
    public GameObject ChatbotUI;
    public GameObject UtilitiesUI;
    public GameObject SpeechBubble;
    public GameObject ArticleObject;
    public GameObject LeaderboardObject;
    public GameObject GraphObject;

    public Image SPencerSprite;
    public Sprite ThinkingSprite;
    public Sprite IdleSprite;
    public ArticleManager articleManager;

    [Header("Microphone")]
    public bool isRecording;
    private AudioClip clip;
    private byte[] bytes;
    private bool recording;

    public Toggle TTSToggle;
    private byte[] EncodeAsWAV(float[] samples, int frequency, int channels)
    {
        using (var memoryStream = new MemoryStream(44 + samples.Length * 2))
        {
            using (var writer = new BinaryWriter(memoryStream))
            {
                writer.Write("RIFF".ToCharArray());
                writer.Write(36 + samples.Length * 2);
                writer.Write("WAVE".ToCharArray());
                writer.Write("fmt ".ToCharArray());
                writer.Write(16);
                writer.Write((ushort)1);
                writer.Write((ushort)channels);
                writer.Write(frequency);
                writer.Write(frequency * channels * 2);
                writer.Write((ushort)(channels * 2));
                writer.Write((ushort)16);
                writer.Write("data".ToCharArray());
                writer.Write(samples.Length * 2);

                foreach (var sample in samples)
                {
                    writer.Write((short)(sample * short.MaxValue));
                }
            }
            return memoryStream.ToArray();
        }
    }
    public void OnToggleRecord()
    {
        if (isRecording)
        {

            isRecording = false;
            var position = Microphone.GetPosition(null);
          //  Microphone.End(null);
            Microphone.End(null);
            var samples = new float[position * clip.channels];
            clip.GetData(samples, 0);
            bytes = EncodeAsWAV(samples, clip.frequency, clip.channels);
            HuggingFaceAPI.AutomaticSpeechRecognition(bytes, OnRecordSuccess, OnRecordFailure);
        }
        else
        {
            isRecording = true;
            clip = Microphone.Start(null, false, 10, 44100);
        }
    }

    public void OnRecordSuccess(string response)
    {
        TextInputfield.text = response;
    }
    public void OnRecordFailure(string response)
    {
        SendBotMessage("Error occured with API callback: " + response);
    }

    public void GoToLeaderboard()
    {
        LeaderboardObject.SetActive(true);
    }


    public void GoToUtilities()
    {
      //  ChatbotUI.SetActive(false);
        UtilitiesUI.SetActive(true);
    }

    private void Awake()
    {
        Sequence seq = DOTween.Sequence();
        seq.Append(SPencerSprite.GetComponent<RectTransform>().DOAnchorPosY(50, 0.05f));
        seq.Append(SPencerSprite.GetComponent<RectTransform>().DOAnchorPosY(5, 0.5f));
        seq.AppendCallback(() => GreetUser());
        Application.targetFrameRate = 120;
    }


    public void ClearAndResetChat()
    {
        foreach(GameObject GO in InstantiatedTextboxes)
        {
            Destroy(GO);
        }

        InstantiatedTextboxes.Clear();
        GreetUser();
    }

    public void GreetUser()
    {
        SendBotMessage(GreetingsMessage);
    }


    public List<string> PastSuccessfulInputs, PastSuccessfulGenerations = new();
    public void OnClickSendMessage()
    {
        if (string.IsNullOrEmpty(TextInputfield.text)) return;
        ContextedText = Context + InputHeader + TextInputfield.text + InputCloser + Seperator;
        
        temptext = TextInputfield.text;
        CHangeSPencerToThinking();
        SendYourMessage(temptext);

        CBM_SentenceTransform.OnApply(temptext);

        TextInputfield.text = "";
        SendButton.interactable = false;
     
    }

    public void OnSTSuccess()
    {
        if(!CBM_SentenceTransform.FindHighestFactor())
        {
            // Respond Normally;
            HuggingFaceAPI.TextGeneration(ContextedText, OnSendMessageSuccess, OnSendMessageFailure);
        }
    }

    public void OnSendMessageSuccess(string response)
    {
        currentResponse = response;
        PastSuccessfulGenerations.Add(response);
        PastSuccessfulInputs.Add(temptext);
        SendBotMessage(response);
    }

    public void OnSendMessageFailure(string response)
    {
        SendButton.interactable = true;
    }

    public string ExtractStringAfterSeparator(string s)
    {
        if (!s.Contains(Seperator)) return s;
        int separatorIndex = s.IndexOf(Seperator);

        // Check if the separator was found
        if (separatorIndex != -1)
        {
            // Extract and return the substring after the separator
            // Adding the length of the separator to skip over it
            return s.Substring(separatorIndex + Seperator.Length);
        }
        else
        {
            // Separator not found, return an empty string or the original string,
            // depending on your needs. Here, returning an empty string as an example.
            return string.Empty;
        }
    }

    public void ReenableButton()
    {
 
    }

    public void SendYourMessage(string s)
    {
        GameObject GO = GameObject.Instantiate(YourMessagePrefab);
        GO.transform.SetParent(SpeechBubblesParent, false);
        InstantiatedTextboxes.Add(GO);
        SpeechBubblePrefab BubblePrefab = GO.GetComponent<SpeechBubblePrefab>();
        BubblePrefab.typewrite = false;
        BubblePrefab.mainChatbot = this;
        BubblePrefab.OnDisplaySpeech(s);

        BubblePrefab.FinishSpeech.AddListener(() => { CHangeSPencerToIdle(); });
    }

    public void SendMultipleArticles(List<Challenge> Cs)
    {
        StartCoroutine(SendMultipleArticlesCoroutine(Cs));
    }

    IEnumerator SendMultipleArticlesCoroutine(List<Challenge> Cs)
    {
       for(int i = 0; i < 3; i++) {
            if (Cs.Count <= i) yield break;
            Challenge c = Cs[i];
            SendArticleWithSummarizeButton(c.NameOfChallenge, CustomButtonPrefab, c, SummarizeArticleIconSprite);
            yield return new WaitForSeconds(1.5f);
        }  
    }


    public void SendChallengeRecommendations(string s, Challenge C)
    {
        GameObject GO = GameObject.Instantiate(ResponseChallengePrefab);
        GO.transform.SetParent(SpeechBubblesParent, false);
        SpeechBubblePrefab BubblePrefab = GO.GetComponent<SpeechBubblePrefab>();
        BubblePrefab.image.sprite = C.ChallengeSprite;
        InstantiatedTextboxes.Add(GO);
        BubblePrefab.SavedChallenge = C;
        BubblePrefab.image.GetComponent<Button>().onClick.AddListener(delegate { GoToArticle(BubblePrefab.SavedChallenge); });
        BubblePrefab.mainChatbot = this;
        BubblePrefab.typewrite = true;
        BubblePrefab.OnDisplaySpeech(ExtractStringAfterSeparator(s));
        BubblePrefab.FinishSpeech.AddListener(() => { CHangeSPencerToIdle(); });
    }

    public void SendBotMessage(string s)
    {
        GameObject GO = GameObject.Instantiate(ResponsePrefab);
        GO.transform.SetParent(SpeechBubblesParent, false);
        InstantiatedTextboxes.Add(GO);
        SpeechBubblePrefab BubblePrefab = GO.GetComponent<SpeechBubblePrefab>();
        BubblePrefab.typewrite = true;
        BubblePrefab.mainChatbot = this;
        BubblePrefab.OnDisplaySpeech(ExtractStringAfterSeparator(s));
        BubblePrefab.FinishSpeech.AddListener(() => { CHangeSPencerToIdle(); });
    }

    public void CHangeSPencerToIdle()
    {
        SpeechBubble.SetActive(false);
        SPencerSprite.sprite = IdleSprite;
    }

    public void CHangeSPencerToThinking()
    {
        SpeechBubble.SetActive(true);
        SPencerSprite.sprite = ThinkingSprite;
    }

    public void SummarizeArticle(Challenge C)
    {
        SendBotMessage("Summarizing article...");
        CBM_Summarizer.Summarize(C.ChallengeDescription);
    }

    public void GoToArticle(Challenge C)
    {
        ArticleObject.SetActive(true);
        articleManager.LoadChallenge(C);
    }

    public void ReturnToPlant()
    {
        SceneManager.LoadScene("HomePage");
    }

    public void SendArticleWithSummarizeButton(string title, GameObject Buttonprefab, Challenge C, Sprite iconSprite)
    {
        GameObject GO = GameObject.Instantiate(ResponsePrefab);
        GameObject GOprefab = GameObject.Instantiate(Buttonprefab);
        GameObject GOImageButton = GameObject.Instantiate(ImageWithButtonprefab);
        InstantiatedTextboxes.Add(GO);
        GO.transform.SetParent(SpeechBubblesParent, false);
        SpeechBubblePrefab BubblePrefab = GO.GetComponent<SpeechBubblePrefab>();
        GOprefab.transform.SetParent(BubblePrefab.BubbleCustomButtonprefabParent, false);
        GOImageButton.transform.SetParent(BubblePrefab.BubbleCustomButtonprefabParent, false);
        CustomResponseButton responseButton = GOprefab.GetComponent<CustomResponseButton>();
        GOImageButton.GetComponent<Image>().sprite = C.ChallengeSprite;
        responseButton.HeldChallenge = C;
        GOImageButton.GetComponent<Button>().onClick.AddListener(delegate { GoToArticle(responseButton.HeldChallenge); });
        responseButton.btn.onClick.AddListener(delegate { SummarizeArticle(responseButton.HeldChallenge); });
        responseButton.Icon.sprite = iconSprite;
        GOImageButton.transform.SetAsLastSibling();
        GOprefab.transform.SetAsLastSibling();
        BubblePrefab.mainChatbot = this;
        BubblePrefab.typewrite = true;
        BubblePrefab.OnDisplaySpeech(ExtractStringAfterSeparator(title));
        BubblePrefab.FinishSpeech.AddListener(() => { CHangeSPencerToIdle(); });

        responseButton.text.text = "Summarize Article";

        CHangeSPencerToThinking();
    }

    public void SendMessageWithCustomButton(string s, GameObject Buttonprefab, string buttonName, UnityAction action, Sprite iconSprite = null)
    {
        GameObject GO = GameObject.Instantiate(ResponsePrefab);
        GameObject GOprefab = GameObject.Instantiate(Buttonprefab);
        InstantiatedTextboxes.Add(GO);
        GO.transform.SetParent(SpeechBubblesParent, false);
        SpeechBubblePrefab BubblePrefab = GO.GetComponent<SpeechBubblePrefab>();
        GOprefab.transform.SetParent(BubblePrefab.BubbleCustomButtonprefabParent, false);

        CustomResponseButton responseButton = GOprefab.GetComponent<CustomResponseButton>();

        responseButton.btn.onClick.AddListener(action);
        responseButton.Icon.sprite = iconSprite;
        GOprefab.transform.SetAsLastSibling();
        BubblePrefab.mainChatbot = this;
        BubblePrefab.typewrite = true;
        BubblePrefab.OnDisplaySpeech(ExtractStringAfterSeparator(s));
        BubblePrefab.FinishSpeech.AddListener(() => { CHangeSPencerToIdle(); });

        responseButton.text.text = buttonName;

        CHangeSPencerToThinking();
    }
}

