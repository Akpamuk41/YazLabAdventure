namespace YazLab.Core
{
    /// <summary>Tekrarlanabilir testlerde animasyon beklemeden başlangıç durumuna döndürülebilen nesne.</summary>
    public interface IResettable
    {
        void ResetState();
    }
}
