using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    // Método para iniciar o jogo
    public void PlayGame()
    {
        // Carrega a cena do jogo (substitua "GameScene" pelo nome da sua cena)
        SceneManager.LoadScene("GameScene");
    }

    // Método para sair do jogo
    public void QuitGame()
    {
        // Se estiver no editor, para a execução
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
            // Se for um build executável, fecha o jogo
            Application.Quit();
#endif
    }
}