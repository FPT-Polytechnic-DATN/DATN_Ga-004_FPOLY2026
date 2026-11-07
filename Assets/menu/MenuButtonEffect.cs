
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class MenuButtonEffect : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
    IPointerClickHandler
{
    [Header("Mau chu")]
    public TMP_Text buttonText;
    public Color normalColor = Color.white;
    public Color hoverColor = Color.red;

    [Header("Hieu ung mau")]
    public Image bloodEffect;
    public CanvasGroup bloodCanvasGroup;

    private bool isPlaying = false;

    private void Start()
    {
        if (buttonText != null)
            buttonText.color = normalColor;

        if (bloodEffect != null)
            bloodEffect.gameObject.SetActive(false);
    }

    // Khi dua chuot vao nut
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (buttonText != null)
            buttonText.color = hoverColor;
    }

    // Khi dua chuot ra khoi nut
    public void OnPointerExit(PointerEventData eventData)
    {
        if (buttonText != null)
            buttonText.color = normalColor;
    }

    // Khi nhan nut
    public void OnPointerClick(PointerEventData eventData)
    {
        if (!isPlaying && bloodEffect != null)
        {
            StartCoroutine(PlayBloodEffect());
        }
    }

    IEnumerator PlayBloodEffect()
    {
        isPlaying = true;

        bloodEffect.gameObject.SetActive(true);

        RectTransform rect =
            bloodEffect.GetComponent<RectTransform>();

        rect.localScale = Vector3.one * 0.3f;

        if (bloodCanvasGroup != null)
            bloodCanvasGroup.alpha = 1f;

        float duration = 0.5f;
        float time = 0f;

        while (time < duration)
        {
            time += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(time / duration);

            // Phong to hieu ung mau
            rect.localScale = Vector3.Lerp(
                Vector3.one * 0.3f,
                Vector3.one * 1.5f,
                t
            );

            // Lam mo dan
            if (bloodCanvasGroup != null)
                bloodCanvasGroup.alpha = 1f - t;

            yield return null;
        }

        bloodEffect.gameObject.SetActive(false);
        isPlaying = false;
    }
}
