public interface ISaveable
{
    // Must return a stable unique ID (from PersistentID)
    string GetUniqueID();

    // Capture current object state
    SaveObjectState CaptureState();

    // Restore object state from save
    void RestoreState(SaveObjectState state);
}
