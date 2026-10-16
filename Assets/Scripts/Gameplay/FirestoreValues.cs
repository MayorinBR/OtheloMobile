namespace Othello.Unity
{
    /// <summary>
    /// Small conversions shared by every Firestore-backed service
    /// (<see cref="FirebaseCloudSaveService"/>, <see cref="FirebaseOnlineMatchService"/>,
    /// <see cref="FirebaseMoveRelay"/>) instead of each duplicating them.
    /// </summary>
    internal static class FirestoreValues
    {
        /// <summary>
        /// Firestore round-trips a whole number as a <c>long</c>, not an
        /// <c>int</c> — every caller that reads a numeric field back out of
        /// a document needs this same conversion.
        /// </summary>
        public static int ToInt(object value, int fallback = 0) => value switch
        {
            long asLong => (int)asLong,
            int asInt => asInt,
            double asDouble => (int)asDouble,
            _ => fallback
        };
    }
}
