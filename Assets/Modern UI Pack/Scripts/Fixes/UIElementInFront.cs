using UnityEngine;

namespace Michsky.UI.ModernUIPack
{
    public class UIElementInFront : MonoBehaviour
    {
        void Start()
        {
            transform.SetAsLastSibling();
        }
    }
}