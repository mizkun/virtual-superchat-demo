using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MessageCubeManager : MonoBehaviour {
    public Text _message;
    public Text _author;

    public void CreateMessage(string message, string author)
    {
        _message.text = message;
        _author.text = author;
    }
}
