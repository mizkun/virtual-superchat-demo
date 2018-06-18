using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System;
using System.Net;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Networking;
using SimpleJSON;
using UnityEngine.UI;

public class YoutubeLiveController : MonoBehaviour
{
    string videoId = "";
    public GameObject messageCubePrefab;
    public string myClientId;
    public string myClientSceret;
    public InputField videoIdField;
    public GameObject inputCanvas;

    public GameObject[] gifts;

    IEnumerator Start()
    {
        // Input video Id 
        yield return new WaitUntil(() => videoId != "");

        var clientId = myClientId;
        var clientSecret = myClientSceret;

        var code = "";
        LocalServer(c => code = c);

        var authUrl = "https://accounts.google.com/o/oauth2/v2/auth?response_type=code"
          + "&client_id=" + clientId
          + "&redirect_uri=" + "http://localhost:8080"
          + "&scope=" + "https://www.googleapis.com/auth/youtube.readonly"
          + "&access_type=" + "offline";
        Application.OpenURL(authUrl);
        yield return new WaitUntil(() => code != "");

        Debug.Log(code);

        var tokenUrl = "https://www.googleapis.com/oauth2/v4/token";
        var content = new Dictionary<string, string>() {
          { "code", code },
          { "client_id", clientId },
          { "client_secret", clientSecret },
          { "redirect_uri",  "http://localhost:8080" },
          { "grant_type", "authorization_code" },
          { "access_type", "offline" },
        };
        var request = UnityWebRequest.Post(tokenUrl, content);
        yield return request.SendWebRequest();

        var json = JSON.Parse(request.downloadHandler.text);
        var token = json["access_token"].RawString();

        Debug.Log(json);
        Debug.Log(token);

        var url = "https://www.googleapis.com/youtube/v3/liveBroadcasts?part=snippet";
        url += "&id=" + videoId;

        Debug.Log(url);

        var req = UnityWebRequest.Get(url);
        req.SetRequestHeader("Authorization", "Bearer " + token);
        yield return req.SendWebRequest();

        json = JSON.Parse(req.downloadHandler.text);
        var chatId = json["items"][0]["snippet"]["liveChatId"].RawString();

        Debug.Log(chatId);

        string new_message_id = "0";
        string previous_message_id = "1";

        string new_superchat_id = "0";
        string previous_superchat_id = "1";

        while (true)
        {
            // Get Message
            url = "https://www.googleapis.com/youtube/v3/liveChat/messages?part=snippet,authorDetails";
            url += "&liveChatId=" + chatId;

            req = UnityWebRequest.Get(url);
            req.SetRequestHeader("Authorization", "Bearer " + token);
            yield return req.SendWebRequest();

            json = JSON.Parse(req.downloadHandler.text);
            var items = json["items"];

            string snip = "";
            string author = "";

            foreach (var item in items)
            {
                snip = item.Value["snippet"]["displayMessage"].RawString();
                author = item.Value["authorDetails"]["displayName"].RawString();
                new_message_id = item.Value["id"].RawString();
                Debug.Log(snip + ":" + author);
                Debug.Log(new_message_id);
            }

            // Spawn message cube.
            if (new_message_id != previous_message_id)
            {
                Vector3 position = new Vector3(UnityEngine.Random.Range(-3.0f, 3.0f), 5, UnityEngine.Random.Range(-3.0f, 3.0f));
                GameObject messageCube = Instantiate(messageCubePrefab, position, Quaternion.identity);
                messageCube.GetComponent<MessageCubeManager>().CreateMessage(snip, author);
            }
            previous_message_id = new_message_id;
            yield return new WaitForSeconds(5f);

            // Get SuperChat Message
            url = "https://www.googleapis.com/youtube/v3/superChatEvents?part=snippet&maxResults=1";

            req = UnityWebRequest.Get(url);
            req.SetRequestHeader("Authorization", "Bearer " + token);
            yield return req.SendWebRequest();

            json = JSON.Parse(req.downloadHandler.text);
            items = json["items"];

            Debug.Log(items);

            long amountMicros = 0;

            foreach (var item in items)
            {
                snip = item.Value["snippet"]["commentText"].RawString();
                author = item.Value["snippet"]["supporterDetails"]["displayName"].RawString();
                new_superchat_id = item.Value["id"].RawString();
                amountMicros = long.Parse(item.Value["snippet"]["amountMicros"].RawString());

                Debug.Log(snip + ":" + author);
                Debug.Log(new_superchat_id);
                Debug.Log(amountMicros);
            }

            if (new_superchat_id != previous_superchat_id)
            {
                Vector3 position = new Vector3(UnityEngine.Random.Range(-3.0f, 3.0f), 5, UnityEngine.Random.Range(-3.0f, 3.0f));
                GameObject gift;
                if (amountMicros <= 100000000)
                {
                    gift = Instantiate(gifts[0], position, Quaternion.identity);
                }
                else if (amountMicros > 100000000 && amountMicros <= 200000000)
                {
                    gift = Instantiate(gifts[1], position, Quaternion.identity);
                }
                else if (amountMicros > 200000000 && amountMicros <= 500000000)
                {
                    gift = Instantiate(gifts[2], position, Quaternion.identity);
                }
                else if (amountMicros > 500000000)
                {
                    gift = Instantiate(gifts[3], position, Quaternion.identity);
                }
                Debug.Log("New SuperChat");
            }
            previous_superchat_id = new_superchat_id;

        }
    }

    public void videoIdReady()
    {
        videoId = videoIdField.text;
        Destroy(inputCanvas);
    }

    void LocalServer(Action<string> onReceive)
    {
        ThreadStart start = () => {
            try
            {
                var listener = new HttpListener();
                listener.Prefixes.Add("http://*:8080/");
                listener.Start();

                var context = listener.GetContext();
                var req = context.Request;
                var res = context.Response;

                var re = new Regex(@"/\?code=(?<c>.*)");
                var code = re.Match(req.RawUrl).Groups["c"].ToString();
                onReceive(code);

                res.StatusCode = 200;
                res.Close();
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
        };
        new Thread(start).Start();
    }

}

public static class SimpleJsonUtility
{
    public static string RawString(this JSONNode node)
    {
        var len = node.ToString().Length - 2;
        return node.ToString().Substring(1, len);
    }
}