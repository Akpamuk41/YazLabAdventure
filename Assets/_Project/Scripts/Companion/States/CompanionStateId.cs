namespace YazLab.Companion
{
    public enum CompanionStateId
    {
        Idle,
        Follow,
        MoveToTarget,
        Blocked,
        Interact
    }

    public static class CompanionStateIdExtensions
    {
        public static string ToDisplayName(this CompanionStateId id) => id switch
        {
            CompanionStateId.Idle => "Bekliyor",
            CompanionStateId.Follow => "Takip ediyor",
            CompanionStateId.MoveToTarget => "Hedefe gidiyor",
            CompanionStateId.Blocked => "Engel algılandı, bekliyor",
            CompanionStateId.Interact => "Etkileşimde",
            _ => id.ToString()
        };
    }
}
