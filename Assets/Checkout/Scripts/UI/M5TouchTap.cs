using UnityEngine;
using UnityEngine.InputSystem;
using ETouch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

namespace DecoAR.M5
{
    /// Lecture d'un doigt ou de la souris pour détecter un « toucher » sur un objet 3D (PC admin, vendeur).
    /// Sur téléphone il n'y a pas de souris : le doigt levé se lit sur la phase du toucher (Ended), et si ce
    /// toucher a déjà disparu, on considère le doigt levé à sa dernière position.
    /// EnhancedTouchSupport doit être activé par l'appelant.
    public static class M5TouchTap
    {
        /// false : aucune entrée à lire. touchPress / lastTouch : état gardé par l'appelant.
        public static bool Read(ref bool touchPress, ref Vector2 lastTouch, out Vector2 pos, out bool down)
        {
            if (ETouch.activeTouches.Count > 0)
            {
                var t = ETouch.activeTouches[0];
                pos = lastTouch = t.screenPosition;
                down = t.phase != TouchPhase.Ended && t.phase != TouchPhase.Canceled;
                touchPress = down;
                return true;
            }
            if (touchPress)
            {
                // le doigt est parti entre deux images
                touchPress = false;
                pos = lastTouch; down = false;
                return true;
            }
            if (Mouse.current != null)
            {
                pos = Mouse.current.position.ReadValue();
                down = Mouse.current.leftButton.isPressed;
                return true;
            }
            pos = default; down = false;
            return false;
        }
    }
}
