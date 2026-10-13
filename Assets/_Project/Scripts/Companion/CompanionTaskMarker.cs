using UnityEngine;
using YazLab.Core;

namespace YazLab.Companion
{
    /// <summary>
    /// Bir etkileşimli nesneyi "yoldaşa verilebilecek görev" olarak işaretler.
    /// Oyuncu emir verdiğinde en yakın uygun görev seçilir.
    /// </summary>
    public class CompanionTaskMarker : MonoBehaviour
    {
        [SerializeField] private string commandLabel = "Şalteri indir";
        [SerializeField] private float commandRange = 60f;

        public string CommandLabel => commandLabel;
        public float CommandRange => commandRange;
        public IInteractable Interactable { get; private set; }

        private void Awake() => Interactable = GetComponent<IInteractable>();
    }
}
