
using UnityEngine;

public class ProbabilityController : MonoBehaviour
{
    public int GetProbability(float value)
    {
        if (value < 1 || value > 100)
            return 0;

        return (int)value;
    }

    public float GetReward(int probability)
    {
        if (probability < 1 || probability > 100)
            return 0;

        return 100f / probability;
    }
}
