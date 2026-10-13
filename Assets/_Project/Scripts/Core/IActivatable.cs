namespace YazLab.Core
{
    /// <summary>Bir tetikleyici (şalter, NPC kararı vb.) tarafından açılıp kapatılabilen mekanizma.</summary>
    public interface IActivatable
    {
        bool IsActive { get; }

        void Activate();

        void Deactivate();
    }
}
