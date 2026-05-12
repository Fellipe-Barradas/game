using UnityEngine;

public class WeaponEquipper : MonoBehaviour
{
    void Start()
    {
        // 1. Pergunta ao State qual arma foi escolhida
        WeaponData armaParaEquipar = GameStateManager.Instance.SelectedWeapon;

        if (armaParaEquipar != null && armaParaEquipar.weaponPrefab != null)
        {
            // 2. Cria a arma como filha da mão (neste objeto)
            GameObject armaInstanciada = Instantiate(armaParaEquipar.weaponPrefab, transform);
            
            // 3. Aplica a "Pegada" de Gameplay que definimos no ScriptableObject
            armaInstanciada.transform.localPosition = armaParaEquipar.handPositionOffset;
            armaInstanciada.transform.localRotation = Quaternion.Euler(armaParaEquipar.handRotationOffset);

            // 4. (Opcional) Passa os dados da arma para o CombatScript
            CombatScript combat = GetComponentInParent<CombatScript>();
            if (combat != null) combat.currentWeapon = armaParaEquipar;
        }
    }
}