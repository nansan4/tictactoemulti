using TMPro;
using UnityEngine;

public class PlayerUI : MonoBehaviour
{
    [SerializeField] private GameObject crossArrow;
    [SerializeField] private GameObject circleArrow;
    [SerializeField] private GameObject crossText;
    [SerializeField] private GameObject circleText;
    [SerializeField] private TextMeshProUGUI crossScoreText;
    [SerializeField] private TextMeshProUGUI circleScoreText;


    private void Awake()
    {
        crossArrow.SetActive(false);
        circleArrow.SetActive(false);
        crossText.SetActive(false);
        circleText.SetActive(false);

        crossScoreText.text = "";
        circleScoreText.text = "";

    }

    private void Start()
    {
        GameManager.Instance.OnGameStarted += GameManager_OnGameStarted;
        GameManager.Instance.OnCurrentPlayablePlayerTypeChanged += GameManager_OnCurrentPlayablePlayerTypeChanged;
        //GameManager.Instance.onGameWin += GameManager_onGameWin;
        GameManager.Instance.OnScoreChanged += GameManager_onScoreChanged;
    }

    private void GameManager_onScoreChanged(object sender, System.EventArgs e)
    {
        GameManager.Instance.GetScores(out int playerCrossScore, out int playerCircleScore);
        crossScoreText.text = playerCrossScore.ToString();
        circleScoreText.text = playerCircleScore.ToString();
    }

    //private void GameManager_onGameWin(object sender, GameManager.OnGameWinEventArgs e)
    //{
    //    GameManager.Instance.GetScores(out int playerCrossScore, out int playerCircleScore);
    //    crossScoreText.text = playerCrossScore.ToString();
    //    circleScoreText.text = playerCircleScore.ToString();
    //}

    private void GameManager_OnCurrentPlayablePlayerTypeChanged(object sender, System.EventArgs e)
    {
        UpdateCurrentArrow();
    }
    private void GameManager_OnGameStarted(object sender, System.EventArgs e)
    {
        if(GameManager.Instance.GetLocalPlayerType() == GameManager.PlayerType.Cross)
        {
            crossText.SetActive(true);
        }
        else
        {
            circleText.SetActive(true);
        }
        crossScoreText.text = "0";
        circleScoreText.text = "0";
        UpdateCurrentArrow();
    }

    private void UpdateCurrentArrow()
    {
        if(GameManager.Instance.GetCurrentPlayablePlayerType() == GameManager.PlayerType.Cross)
        {
            crossArrow.SetActive(true);
            circleArrow.SetActive(false);
        } else
        {
            crossArrow.SetActive(false);
            circleArrow.SetActive(true);
        }
    }
}
