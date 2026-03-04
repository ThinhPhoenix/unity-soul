using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerHealthController : MonoBehaviour
{
    public HealthBarScript healthBarScript;
    public float luongMauHienTai;
    public float luongMauToiDa = 10;
    public float tocDoGiamMau = 0.5f;

    // New field to prevent multiple deaths
    private bool isDead = false;
    
    // Reference to the GameStateManager
    private GameStateManager gameStateManager;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        luongMauHienTai = luongMauToiDa;
        healthBarScript.capNhatThanhMau(luongMauHienTai, luongMauToiDa);
        
        // Try to find the GameStateManager
        gameStateManager = FindObjectOfType<GameStateManager>();
        if (gameStateManager == null)
        {
            Debug.LogWarning("GameStateManager not found in scene. Adding one now.");
            
            // Create a new GameStateManager if none exists
            GameObject gsmObject = new GameObject("GameStateManager");
            gameStateManager = gsmObject.AddComponent<GameStateManager>();
        }
    }

    // Xóa hoặc vô hiệu hóa phương thức OnMouseDown để ngăn không cho người chơi tự mất máu khi nhấn chuột trái
    // private void OnMouseDown()
    // {
    //     luongMauHienTai = luongMauHienTai - 1;
    //     healthBarScript.capNhatThanhMau(luongMauHienTai, luongMauToiDa);
    // }

    // New method to take damage from rocks
    public void TakeDamage(float damage)
    {
        // Prevent damage if already dead
        if (isDead)
            return;
            
        // Thêm log để debug
        Debug.Log($"Player taking damage: {damage}. Called from: {new System.Diagnostics.StackTrace().ToString()}");
        
        luongMauHienTai -= damage;
        
        // Make sure health doesn't go below zero
        if (luongMauHienTai < 0)
        {
            luongMauHienTai = 0;
        }
        
        // Update the health bar
        healthBarScript.capNhatThanhMau(luongMauHienTai, luongMauToiDa);
        
        // IMPORTANT: Add this line to trigger animation on the PlayerController
        GetComponent<PlayerController>()?.TakeDamage(damage);
        
        // Check if player died
        if (luongMauHienTai <= 0 && !isDead)
        {
            Debug.Log("Player died!");
            isDead = true;

            if (gameStateManager != null)
            {
                gameStateManager.ForceGameOver();
                return;
            }

            StartCoroutine(HandlePlayerDeath());
        }
    }

    // Fallback only when GameStateManager is unavailable
    private System.Collections.IEnumerator HandlePlayerDeath()
    {
        yield return new WaitForSeconds(0.5f);

        if (gameStateManager != null)
        {
            yield break;
        }

        Debug.LogWarning("GameStateManager is missing. Loading lose scene as fallback.");
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 2);
    }

    //void Update()
    //{
    //    // Giảm máu theo thời gian
    //    luongMauHienTai -= tocDoGiamMau * Time.deltaTime;

    //    // Đảm bảo lượng máu không nhỏ hơn 0
    //    if (luongMauHienTai < 0)
    //    {
    //        luongMauHienTai = 0;
    //        Destroy(this.gameObject);
    //    }

    //    // Cập nhật thanh máu
    //    healthBarScript.capNhatThanhMau(luongMauHienTai, luongMauToiDa);
    //}
}