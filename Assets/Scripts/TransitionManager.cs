using PlayerControllers;
using UnityEngine;

public class TransitionManager : MonoBehaviour
{
    public GameObject[] levels;
    public int levelNumber;
    public GameObject player;
    public Transform spawnPoint;

    private const string LevelKey = "CurrentLevel";

    public void Awake()
    {
        levelNumber = PlayerPrefs.GetInt(LevelKey, levelNumber);

        // Make sure the saved level is valid
        levelNumber = Mathf.Clamp(levelNumber, 0, levels.Length - 1);

        foreach (var obj in levels)
        {
            obj.SetActive(false);
        }

        levels[levelNumber].SetActive(true);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            FPP_PlayerController _player = player.GetComponent<FPP_PlayerController>();

            if (!_player.isDead)
            {
                levelNumber ++;

                foreach (var obj in levels)
                {
                    obj.SetActive(false);
                }

                levels[levelNumber].SetActive(true);

                player.transform.position = spawnPoint.transform.position;
                player.transform.rotation = spawnPoint.transform.rotation;

                _player.ResetVariables();

                // Save the current level
                PlayerPrefs.SetInt(LevelKey, levelNumber);
                PlayerPrefs.Save();
            }
        }
    }
}