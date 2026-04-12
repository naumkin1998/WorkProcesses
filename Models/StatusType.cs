namespace WorkProcesses.Models
{
    /// <summary>
    /// Типы статусов сотрудника
    /// </summary>
    public enum StatusType
    {
        Present,        // 0 - На работе (зелёный)
        Remote,         // 1 - Удалённая работа (зелёный с пометкой)
        Absent,         // 2 - Отсутствует (красный)
        Vacation,       // 3 - В отпуске (жёлтый)
        Sick,           // 4 - На больничном (жёлтый)
        BusinessTrip    // 5 - В командировке (жёлтый)
    }
}
