using UnityEngine;

// Estado de desbloqueio das classes, persistido em PlayerPrefs.
// Espelha o padrão de chaves por classe usado em WeaponTierManager / GerenciadorMoedas.
public static class ProgressaoClasses
{
    private const string PREFIXO = "ClasseDesbloqueada_";

    public static bool EstaDesbloqueada(PlayerClass classe)
    {
        if (classe == PlayerClass.Espadachim) return true; // inicial, sempre livre
        return PlayerPrefs.GetInt(PREFIXO + classe, 0) == 1;
    }

    public static void Desbloquear(PlayerClass classe)
    {
        PlayerPrefs.SetInt(PREFIXO + classe, 1);
        PlayerPrefs.Save();
    }
}
