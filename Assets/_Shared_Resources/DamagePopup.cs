using UnityEngine;
using TMPro;

public class DamagePopup : MonoBehaviour
{
    private TextMeshPro textMesh;
    private float lifetime = 1f;
    private float timer;
    private Vector3 moveDirection;
    private float moveSpeed = 2f;
    
    private static TMP_FontAsset cachedFont;
    
    public void Setup(int damageAmount, bool isPlayer = false, TMP_FontAsset customFont = null)
    {
        textMesh = GetComponent<TextMeshPro>();
        if (textMesh == null)
        {
            textMesh = gameObject.AddComponent<TextMeshPro>();
        }

        if (customFont != null)
        {
            textMesh.font = customFont;
        }

        textMesh.text = damageAmount.ToString();
        textMesh.fontSize = 5;
        textMesh.alignment = TextAlignmentOptions.Center;
        
        // Base color
        textMesh.color = isPlayer ? Color.red : Color.yellow;
        
        textMesh.sortingOrder = 100;

        timer = lifetime;
        
        // Randomize direction slightly
        float randomX = Random.Range(-0.5f, 0.5f);
        moveDirection = new Vector3(randomX, 1f, 0f).normalized;
    }

    void Update()
    {
        transform.position += moveDirection * moveSpeed * Time.deltaTime;
        timer -= Time.deltaTime;

        if (timer < lifetime / 2)
        {
            // Fade out
            float alpha = timer / (lifetime / 2);
            Color c = textMesh.color;
            c.a = alpha;
            textMesh.color = c;
        }

        if (timer <= 0)
        {
            Destroy(gameObject);
        }
    }
}
