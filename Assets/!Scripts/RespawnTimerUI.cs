using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RespawnTimerUI : MonoBehaviour
{
    [SerializeField] private Image _image;
    [SerializeField] private TMP_Text _timer;

    public void StartTimer()
    {
        _image.gameObject.SetActive(true);
       _timer.gameObject.SetActive(true);
        StartCoroutine(RespawnAfterDelay());
    }
    private IEnumerator RespawnAfterDelay()
    {
        _timer.text = "3";
        yield return new WaitForSeconds(1);
        _timer.text = "2";
        yield return new WaitForSeconds(1);
        _timer.text = "1";
        yield return new WaitForSeconds(1);
        _timer.text = "0";
        _timer.gameObject.SetActive(false);
        _image.gameObject.SetActive(false);
    }
}
