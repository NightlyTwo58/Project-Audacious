using UnityEngine;
using TMPro;

public class SignScript : MonoBehaviour
{
    public TextMeshProUGUI scoreText;

    public PlayerMovement playerScript;
    public EnemyMovement enemyScript;

    public string scorePrefix = "The score is: \n";

    void Start()
    {
        // Basic error checking to make sure references are set
        if (scoreText == null)
        {
            Debug.LogError("Score Text (TMP) not assigned in SignScript! Please assign it in the Inspector.");
            enabled = false;
            return;
        }

        if (playerScript == null)
        {
            Debug.LogError("Player Script not assigned in SignScript! Please assign your Player's PlayerMovement script in the Inspector.");
            enabled = false;
            return;
        }

        // Initial update of the score display
        UpdateScoreDisplay();
    }

    void Update()
    {
        UpdateScoreDisplay();
    }

    void UpdateScoreDisplay()
    {
        scoreText.text = scorePrefix + "Player: " + (enemyScript.deaths).ToString() + " Bot: " + (playerScript.deaths).ToString();
    }
}