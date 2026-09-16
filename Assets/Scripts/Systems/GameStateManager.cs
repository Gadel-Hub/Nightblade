using UnityEditor;
using UnityEngine;


public enum GameState
{
    Playing,
    Paused,
    GameOver
}
public class GameStateManager : MonoBehaviour
{
    public GameState State;

    public void StartGame()
    {
        State = GameState.Playing;
    }

    public void Paused()
    {
        State = GameState.Paused;
    }

    public void GameOver()
    {
        State = GameState.GameOver;
    }

    public void ResumeGame()
    {
        
        State = GameState.Playing;
    }

}
