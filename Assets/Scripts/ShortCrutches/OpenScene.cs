using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class OpenScene : MonoBehaviour
{
    public string SceneName = "Menu";
    public void Open()
    {
        SceneManager.LoadScene(SceneName);
    }
}
