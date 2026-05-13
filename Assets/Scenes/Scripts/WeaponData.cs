using UnityEngine;

[CreateAssetMenu(fileName = "NewWeapon", menuName = "Fire Knight/Weapon Data")]
public class WeaponData : ScriptableObject
{
    [Header("Informações Básicas")]
    public string weaponName = "Nova Arma";
    
    [Header("Visual e Pegada (Menu)")]
    public GameObject weaponPrefab;
    public Vector3 menuPositionOffset = Vector3.zero;
    public Vector3 menuRotationOffset = Vector3.zero;
    public Vector3 menuScale = Vector3.one; // Valor padrão 1, 1, 1

    [Header("Visual e Pegada (Gameplay)")]
    public bool equipInLeftHand = false;
    public Vector3 handPositionOffset = Vector3.zero;
    public Vector3 handRotationOffset = Vector3.zero;
    public Vector3 handScale = Vector3.one; // Valor padrão 1, 1, 1

    [Header("Atributos de Combate")]
    public int attackDamage = 20;
    public float attackRate = 1.5f; 
    public float attackRange = 1.5f;

    [Header("Feedbacks Sonoros")]
    public AudioClip swingSound; 
    public AudioClip hitSound;  
}