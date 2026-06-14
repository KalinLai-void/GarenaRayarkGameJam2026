using TMPro;
using UnityEngine;
using UnityEngine.InputSystem.HID;

public class GameEndManager : MonoBehaviour
{
    public static GameEndManager instance;

    [SerializeField] private GameObject HUD;
    [SerializeField] private GameObject PassUI;
    [SerializeField] private GameObject DieUI;
    [SerializeField] private TextMeshProUGUI DieText;
    [SerializeField] private string dieStr = "你已經歷第 X 次死亡";

    private void Start()
    {
        if (instance == null ) instance = this;
    }

    public void PassGame()
    {
        HUD.SetActive(false);
        PassUI.SetActive(true);
    }

    public void DieGame()
    {
        HUD.SetActive(false);
        DieUI.SetActive(true);
        Invoke("ShowDieText", 0.3f);
    }

    private void ShowDieText()
    {
        DieText.text = dieStr.Replace("X", GameManager.GetDieTime().ToString());
        DieText.gameObject.SetActive(true);
    }

    public void BackTitle()
    {
        GameManager.TriggerGoToTitle();
    }
}
