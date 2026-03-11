using System;

namespace CardMatch.Core.Save
{
    [Serializable]
    public class CardSaveData
    {
        public int UniqueInstanceId;
        public int PairId;
        public int BoardIndex;
        public bool IsRevealed;
        public bool IsMatched;
    }
}