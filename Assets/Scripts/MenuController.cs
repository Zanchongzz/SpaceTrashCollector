using System.Collections;
using UnityEngine;

public class MenuController : MonoBehaviour
{
    public AudioSource audioSource;
    public AudioClip clickClip;
    public float delay = 0.3f;

    SetupFlow flow;

    void Start()
    {
        Time.timeScale = 1f;
        flow = FindFirstObjectByType<SetupFlow>();
    }

    public void PlayGame()
    {
        Click();
        if (flow == null) flow = FindFirstObjectByType<SetupFlow>();
        if (flow == null)
        {
            Debug.LogError("Chưa gắn SetupFlow vào MenuManager!");
            return;
        }
        flow.Open();
    }

    public void QuitGame()
    {
        Debug.Log("Đã bấm EXIT");
        Click();
        StartCoroutine(QuitAfterDelay());
    }

    IEnumerator QuitAfterDelay()
    {
        yield return new WaitForSecondsRealtime(delay);
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    void Click()
    {
        if (clickClip == null) return;
        if (audioSource != null) audioSource.PlayOneShot(clickClip);
        else if (Camera.main != null)
            AudioSource.PlayClipAtPoint(clickClip, Camera.main.transform.position);
    }
}