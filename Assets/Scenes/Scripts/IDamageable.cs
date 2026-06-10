/// <summary>
/// Qualquer coisa que pode receber dano (inimigos, boss, futuros tipos).
/// Flecha e melee batem nisto, sem checar tipo concreto.
/// </summary>
public interface IDamageable
{
    void TakeDamage(int dano);
}
