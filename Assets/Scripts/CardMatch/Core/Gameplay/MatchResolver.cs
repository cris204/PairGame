using CardMatch.Core.Data;

namespace CardMatch.Core.Gameplay
{
    public class MatchResolver
    {
        public bool IsMatch(CardRuntimeData first, CardRuntimeData second)
        {
            if (first == null || second == null)
            {
                return false;
            }

            return first.PairId == second.PairId;
        }
    }
}