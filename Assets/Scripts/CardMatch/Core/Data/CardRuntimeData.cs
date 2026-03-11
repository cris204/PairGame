using System;

namespace CardMatch.Core.Data
{
    [Serializable]
    public class CardRuntimeData
    {
        public int UniqueInstanceId;
        public int PairId;
        public int BoardIndex;
        public bool IsRevealed;
        public bool IsMatched;
    }
}