using UnityEngine;

public class WeaponEquipper : MonoBehaviour
{
    void Start()
    {
        WeaponData armaParaEquipar = GameStateManager.Instance.SelectedWeapon;

        if (armaParaEquipar != null && armaParaEquipar.weaponPrefab != null)
        {
            GameObject armaInstanciada = Instantiate(armaParaEquipar.weaponPrefab, transform);
            
            // Log para ver o que está escrito no ScriptableObject
            Debug.Log($"[ARMA] Nome: {armaParaEquipar.weaponName} | Escala no SO: {armaParaEquipar.handScale}");

            armaInstanciada.transform.localPosition = armaParaEquipar.handPositionOffset;
            armaInstanciada.transform.localRotation = Quaternion.Euler(armaParaEquipar.handRotationOffset);
            
            // Aplicando a escala
            armaInstanciada.transform.localScale = armaParaEquipar.handScale;

            // Log para ver se a escala realmente mudou no objeto da cena
            Debug.Log($"[ARMA] Escala aplicada no Objeto: {armaInstanciada.transform.localScale}");

            CombatScript combat = GetComponentInParent<CombatScript>();
            if (combat != null) combat.currentWeapon = armaParaEquipar;
        }
        else
        {
            Debug.LogWarning("[ARMA] Nenhuma arma selecionada ou Prefab faltando!");
        }
    }
}