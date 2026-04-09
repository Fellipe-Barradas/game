using UnityEngine;

// Isso cria um botão no menu do botão direito da Unity para criarmos novas armas facilmente
[CreateAssetMenu(fileName = "NewWeapon", menuName = "Fire Knight/Weapon Data")]
public class WeaponData : ScriptableObject
{
    [Header("Informações Básicas")]
    public string weaponName = "Nova Arma";
    
    [Header("Atributos de Combate")]
    public int attackDamage = 20;
    public float attackRate = 1.5f; // Ataques por segundo
    public float attackRange = 1.5f;

    [Header("Feedbacks Sonoros")]
    public AudioClip swingSound; // Som específico desta arma cortando o ar
    public AudioClip hitSound;   // Som específico desta arma batendo
}
