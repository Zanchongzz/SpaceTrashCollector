using UnityEngine;
using UnityEngine.UI;

public class Soundexit : MonoBehaviour
{
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip clickSound;   // sfx riêng cho EXIT
    [SerializeField] private Button exitButton;

    void Awake()
    {
        if (exitButton == null) exitButton = GetComponent<Button>();
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
    }

    void OnEnable()
    {
        if (exitButton != null) exitButton.onClick.AddListener(PlaySound);
    }

    void OnDisable()
    {
        if (exitButton != null) exitButton.onClick.RemoveListener(PlaySound);
    }

    private void PlaySound()
    {
        if (audioSource != null && clickSound != null)
            audioSource.PlayOneShot(clickSound);
    }
}