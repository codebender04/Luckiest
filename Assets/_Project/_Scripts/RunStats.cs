using System.Collections.Generic;
using UnityEngine;

public class RunStats
{
    public int Wins { get; }
    public int Busts { get; }
    public int BestStreak { get; }
    public int RiskiestWin { get; }        // lowest winning probability, 0 if no wins
    public float LostToBusts { get; }
    public float AverageOdds { get; }
    public double OneInN { get; }          // how unlikely this exact sequence of results was
    public bool[] Wiped { get; }           // wins whose points a later bust took away

    public RunStats(IReadOnlyList<Attempt> attempts)
    {
        Wiped = new bool[attempts.Count];
        double chance = 1d;
        int streak = 0;
        int lastBust = -1;
        float oddsSum = 0f;

        for (int i = 0; i < attempts.Count; i++)
        {
            Attempt a = attempts[i];
            float p = a.Probability / 100f;
            oddsSum += a.Probability;

            if (a.Won)
            {
                Wins++;
                streak++;
                BestStreak = Mathf.Max(BestStreak, streak);
                RiskiestWin = RiskiestWin == 0 ? a.Probability : Mathf.Min(RiskiestWin, a.Probability);
                chance *= p;
            }
            else
            {
                Busts++;
                streak = 0;
                LostToBusts -= a.Delta;
                lastBust = i;
                chance *= 1f - p;
            }
        }

        for (int i = 0; i < lastBust; i++)
            Wiped[i] = attempts[i].Won;

        AverageOdds = attempts.Count > 0 ? oddsSum / attempts.Count : 0f;
        OneInN = chance > 0d ? 1d / chance : double.PositiveInfinity;
    }

    public string Title(float finalScore, int jackpotThreshold)
    {
        if (finalScore <= 0f) return "WIPED OUT";
        if (OneInN >= 1000d) return "MIRACLE RUN";
        if (RiskiestWin > 0 && RiskiestWin <= jackpotThreshold) return "JACKPOT RUN";
        if (Busts == 0) return AverageOdds >= 80f ? "PLAYED IT SAFE" : "FLAWLESS";
        if (LostToBusts > finalScore) return "SO CLOSE";
        if (AverageOdds < 40f) return "HIGH ROLLER";
        return "RUN OVER";
    }
}
