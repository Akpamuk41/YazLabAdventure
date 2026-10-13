using Unity.AI.Navigation;
using UnityEngine;

namespace YazLab.Core
{
    /// <summary>
    /// Köprü gibi yürünebilir geometri değiştiğinde NavMesh'i asenkron olarak günceller.
    /// Güncelleme arka planda yapıldığı için kare hızı düşmez.
    /// </summary>
    [RequireComponent(typeof(NavMeshSurface))]
    public class NavMeshRebuilder : MonoBehaviour
    {
        private NavMeshSurface _surface;
        private AsyncOperation _pending;
        private bool _rebuildQueued;

        private void Awake() => _surface = GetComponent<NavMeshSurface>();

        private void OnEnable() => EventBus.Subscribe<NavMeshDirtyEvent>(OnNavMeshDirty);

        private void OnDisable() => EventBus.Unsubscribe<NavMeshDirtyEvent>(OnNavMeshDirty);

        private void OnNavMeshDirty(NavMeshDirtyEvent e) => _rebuildQueued = true;

        private void Update()
        {
            if (!_rebuildQueued || (_pending != null && !_pending.isDone)) return;

            _rebuildQueued = false;
            if (_surface.navMeshData == null)
            {
                _surface.BuildNavMesh();
                return;
            }

            _pending = _surface.UpdateNavMesh(_surface.navMeshData);
        }
    }
}
