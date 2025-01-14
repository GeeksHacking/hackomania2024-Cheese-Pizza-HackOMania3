using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HuggingFace.API;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
public class Chatbot_ST : MonoBehaviour
{

    [Header("Variables")]
    public Chatbot chatBotMain;
    //I cant turn this into a class because i need a global string[] Var that cant be class encapped;

    public string[] Actions;
    public float[] ResponseSimilarityFactor;
   // public string[] ActionResponses;
    public UnityEvent[] ActionResponseEvents;

    public ChallengesManager challengesManager;

    //God forgive me 
    //ResponseSimilarityFactor is from 0 to 1. find the highest in the array, and correlate it to the string in Actions[]

    public string HighestString;
    public float HighestStringFactor;

    [Range(0, 0.95f)]
    public float SentenceMatchThreshold;

    [Header("Actions and events")]
    public GameObject LeaderboardButtonPrefab;

    [Header("SmokeAndMirrors")]
    public string nearestEVLocation;
    public string LinkToMap;

    [Header("Icons")]
    public Sprite GPSSprite;
    public Sprite GraphSprite, PlantSprite, LeaderboardSprite, ReportSprite, EnergyUsageSprite;


    public void OpenMapOnGoogle()
    {
        Application.OpenURL(LinkToMap);
    }
  public void OnApply(string s)
    {
        HuggingFaceAPI.SentenceSimilarity(s, OnSuccess, OnFailure, Actions);
    }

    public void OnSuccess(float[] f)
    {
        ResponseSimilarityFactor = f;
        chatBotMain.OnSTSuccess();
       // FindHighestFactor();
    }

    public void OnFailure(string s)
    {
        Debug.LogError("Cannot for some reason" + s);
        chatBotMain.SpeechBubble.SetActive(false);
    }

    public void OnReplyWithClosestMatchedArticles()
    {
        challengesManager.ReturnMatchedArticles(chatBotMain.temptext);
        if (challengesManager.QueriedArticles.Count <= 0)
        {
            chatBotMain.SendBotMessage("Sorry, I couldnt find any articles that match what you are looking for. Please try a different topic, or try again later.");
        }
        else
        {


            string responseText = "Here is a list of articles that you might find interesting!\nClick the picture to view the article. Remember, you gain leaves for completing a quiz after viewing the article, or you can ask me to summarize the article for you!";
            chatBotMain.SendBotMessage(responseText);

            chatBotMain.SendMultipleArticles(challengesManager.QueriedArticles);
        }
    }

    public void OnReplyWithClosestChallenge()
    {
        challengesManager.ReturnMatchedChallenges(chatBotMain.temptext);
        if (challengesManager.QueriedChallenges.Count <= 0)
        {
            chatBotMain.SendBotMessage("Sorry, there seems to be no challenges today that utilize those items, try using different keywords when searching for events.");
        }
        else
        {


            string responseText = "Here is a challenge that matches what you might be looking for! \n\n" + challengesManager.QueriedChallenges[0].NameOfChallenge + "\n\nClick to view the challenge! Remember, you can claim rewards for every challenge you complete!";
            chatBotMain.SendChallengeRecommendations(responseText, challengesManager.QueriedChallenges[0]);
        }

    }

    public void CustomNotImplementedException()
    {
        Debug.LogError("Not implemented!");
    }


    public void RespondWithGraph()
    {
        chatBotMain.SendMessageWithCustomButton("Viewing your energy consumption graph over time helps to see your trends in energy usage! Click the button below to view your energy report graph over time.", LeaderboardButtonPrefab, "View Graph", delegate { chatBotMain.GraphObject.SetActive(true); }, GraphSprite);
    }
    public void RespondWithleaderboard()
    {
        chatBotMain.SendMessageWithCustomButton("Click the button below to view the leaderboard!", LeaderboardButtonPrefab, "View Leaderboard", delegate { SceneManager.LoadScene("Leaderboard"); }, LeaderboardSprite);
    }

    public void RespondWithPlantRedirect()
    {
        chatBotMain.SendMessageWithCustomButton("Your plant is going strong! Keep it healthy by completing daily challenges! Click the button below to go to your plant.", LeaderboardButtonPrefab, "View Plant", delegate { SceneManager.LoadScene("PlantScene"); }, PlantSprite);
    }

    public void SearchForNearestChargingStation()
    {

        StartCoroutine(SearchForNearestChargingStationCoroutine());
    }

    IEnumerator SearchForNearestChargingStationCoroutine()
    {
        chatBotMain.SendBotMessage("Searching for the nearest charging station in your area...");
        yield return new WaitForSeconds(1.5f);
        // Check if the user has location service enabled.
        if (!Input.location.isEnabledByUser)
            Debug.Log("Location not enabled on device or app does not have permission to access location");
        Input.location.Start();
        int maxWait = 5;
        while (Input.location.status == LocationServiceStatus.Initializing && maxWait > 0)
        {
            yield return new WaitForSeconds(1);
            maxWait--;
        }
        if (maxWait < 1)
        {
            chatBotMain.SendBotMessage("Im sorry, but I can't access your location at this time. Please try again later.");
            yield break;
        }
        if (Input.location.status == LocationServiceStatus.Failed)
        {
            chatBotMain.SendBotMessage("Im sorry, but I can't access your location at this time. Please try again later.");
            yield break;
        }
        else
        {
            // Access granted and location value could be retrieve
            double longitude = Input.location.lastData.longitude;
            double latitude = Input.location.lastData.latitude;
           // chatBotMain.SendBotMessage("Retrieved user lat and long\n" + longitude.ToString() + "\n" + latitude.ToString());
            yield return new WaitForSeconds(1.0f);
            RespondWithSuccessfulEV();
        }
    }

    public void RespondWithSuccessfulEV()
    {
        chatBotMain.SendMessageWithCustomButton("I have found the nearest charging station for you\n\n" + nearestEVLocation + "\n\nClick the button below to view the trip on google maps", LeaderboardButtonPrefab, "View on map", delegate { OpenMapOnGoogle() ; },GPSSprite);
    }

    public void RespondWithReportSubmission()
    {
        chatBotMain.SendMessageWithCustomButton("Linking your utilies can allow you to trends in your energy usage and save more energy! Click the button below to link your utilities now.", LeaderboardButtonPrefab, "Link Utilities", delegate { chatBotMain.GoToUtilities(); }, ReportSprite);
    }

    public bool FindHighestFactor()
    {
        HighestString = "";
        if (Actions.Length == 0 || ResponseSimilarityFactor.Length == 0 || Actions.Length != ResponseSimilarityFactor.Length)
        {
            Debug.LogError("Arrays are empty or not of the same length!");
            return false;
        }

        float highestValue = ResponseSimilarityFactor[0];
        int highestIndex = 0;

        // Loop through the ResponseSimilarityFactor to find the highest value
        for (int i = 1; i < ResponseSimilarityFactor.Length; i++)
        {
            if (ResponseSimilarityFactor[i] > 0 && ResponseSimilarityFactor[i] > SentenceMatchThreshold && ResponseSimilarityFactor[i] > highestValue)
            {
                highestValue = ResponseSimilarityFactor[i];
                highestIndex = i;
            }
        }

        if (highestValue >= SentenceMatchThreshold)
        {
            // Output the action corresponding to the highest response similarity factor
            Debug.Log($"Action with the highest response similarity factor: {Actions[highestIndex]} ({highestValue})");

            HighestStringFactor = highestValue;
            HighestString = Actions[highestIndex];

            if (highestIndex == 0) return false;
            ActionResponseEvents[highestIndex].Invoke();

            return true;
        }
        return false;

    }
}
