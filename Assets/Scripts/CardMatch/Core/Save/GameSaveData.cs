using System;
using System.Collections.Generic;

namespace CardMatch.Core.Save
{
    [Serializable]
    public class GameSaveData
    {
        public int Rows;
        public int Columns;
        public int Score;
        public int Combo;
        public bool IsGameCompleted;
        public List<CardSaveData> Cards = new List<CardSaveData>();
    }
}