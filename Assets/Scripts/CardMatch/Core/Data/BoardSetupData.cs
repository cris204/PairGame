using System;

namespace CardMatch.Core.Data
{
    [Serializable]
    public struct BoardSetupData
    {
        public int Rows;
        public int Columns;

        public BoardSetupData(int rows, int columns)
        {
            Rows = rows;
            Columns = columns;
        }

        public int TotalCards()
        {
            return Rows * Columns;
        }

        public bool IsValid()
        {
            if (Rows <= 0 || Columns <= 0)
            {
                return false;
            }

            if ((Rows * Columns) < 2)
            {
                return false;
            }

            return true;
        }
    }
}
