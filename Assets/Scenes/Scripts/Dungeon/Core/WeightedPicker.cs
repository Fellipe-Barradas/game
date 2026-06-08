using System.Collections.Generic;

namespace Game.Dungeon
{
    /// <summary>Seleção ponderada pura e determinística (sem dependência de Unity runtime).</summary>
    public static class WeightedPicker
    {
        /// <summary>
        /// Índice escolhido em 'weights' dado um roll em [0,1). Pesos &lt;= 0 são ignorados.
        /// Retorna -1 se não houver peso positivo (ou lista vazia).
        /// </summary>
        public static int PickIndex(IReadOnlyList<float> weights, double roll01)
        {
            float total = 0f;
            for (int i = 0; i < weights.Count; i++)
                if (weights[i] > 0f) total += weights[i];

            if (total <= 0f) return -1;

            double r = roll01 * total;
            for (int i = 0; i < weights.Count; i++)
            {
                if (weights[i] <= 0f) continue;
                r -= weights[i];
                if (r < 0) return i;
            }

            // Salvaguarda contra arredondamento: último com peso positivo.
            for (int i = weights.Count - 1; i >= 0; i--)
                if (weights[i] > 0f) return i;
            return -1;
        }
    }
}
