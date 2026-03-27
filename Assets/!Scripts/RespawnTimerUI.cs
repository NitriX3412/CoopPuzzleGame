using System.Collections;
using TMPro;
using UnityEngine;

public class RespawnTimerUI : MonoBehaviour
{
    [SerializeField] private TMP_Text _timer;

    public void StartTimer()
    {
       _timer.enabled = true;
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
        _timer.enabled = false;
    }
}
