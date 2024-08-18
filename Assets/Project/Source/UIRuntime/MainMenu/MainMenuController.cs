using Manatea.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    [SerializeField]
    private SceneReference m_NewGameScene;


    public void StartGame()
    {
        SceneManager.LoadScene(SceneHelper.GetScenePath(m_NewGameScene));
    }

    public void ExitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.ExitPlaymode();
# else
        Application.Quit();
#endif
    }
}
