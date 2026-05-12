using UnityEngine;

[CreateAssetMenu(fileName = "NewWeapon", menuName = "Fire Knight/Weapon Data")]
public class WeaponData : ScriptableObject
{
    [Header("Informações Básicas")]
    public string weaponName = "Nova Arma";
    
    [Header("Visual e Pegada (Menu)")]
    public GameObject weaponPrefab;
    public Vector3 menuPositionOffset = Vector3.zero; // Ajuste fino de posição na tela de seleção
    public Vector3 menuRotationOffset = Vector3.zero; // Ajuste fino de rotação na tela de seleção

    [Header("Visual e Pegada (Gameplay)")]
    public bool equipInLeftHand = false;
    public Vector3 handPositionOffset = Vector3.zero; // Como fica na mão do personagem
    public Vector3 handRotationOffset = Vector3.zero;

    [Header("Atributos de Combate")]
    public int attackDamage = 20;
    public float attackRate = 1.5f; 
    public float attackRange = 1.5f;

    [Header("Feedbacks Sonoros")]
    public AudioClip swingSound; 
    public AudioClip hitSound;  

    
}