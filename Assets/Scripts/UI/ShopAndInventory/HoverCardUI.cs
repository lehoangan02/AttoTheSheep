using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace AttoTheSheep.UI.ShopAndInventory
{
    public class HoverCardUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI descriptionText;

        public void Show(string title, string description)
        {
            if (titleText != null) titleText.text = title;
            if (descriptionText != null) descriptionText.text = description;
            
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}
