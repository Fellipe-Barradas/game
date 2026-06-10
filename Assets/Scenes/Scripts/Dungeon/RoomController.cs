using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Runtime de uma sala instanciada: ativa inimigos quando o player entra,
/// e (se for sala de combate) tranca as portas até a sala ser limpa.
/// </summary>
public class RoomController : MonoBehaviour
{
    public bool lockDoorsUntilCleared = false;

    private readonly List<GameObject> enemies = new List<GameObject>();
    private readonly List<DoorController> doors = new List<DoorController>();
    private bool activated;
    private bool cleared;

    public void Configure(IEnumerable<GameObject> spawnedEnemies, bool lockDoors)
    {
        lockDoorsUntilCleared = lockDoors;
        enemies.Clear();
        foreach (GameObject e in spawnedEnemies)
        {
            if (e == null) continue;
            e.SetActive(false);
            enemies.Add(e);
        }
        doors.Clear();
    }

    /// <summary>Registra uma porta desta sala (chamado pelo gerador após instanciar as portas).</summary>
    public void RegisterDoor(DoorController d)
    {
        if (d != null && !doors.Contains(d)) doors.Add(d);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (activated || cleared) return;
        if (!other.CompareTag("Player")) return;
        Activate();
    }

    private void Activate()
    {
        activated = true;
        foreach (GameObject e in enemies)
            if (e != null) e.SetActive(true);

        if (lockDoorsUntilCleared && enemies.Count > 0)
            foreach (DoorController d in doors) d.SetLocked(true);
    }

    private void Update()
    {
        if (!activated || cleared) return;
        enemies.RemoveAll(e => e == null);
        if (enemies.Count == 0)
        {
            cleared = true;
            foreach (DoorController d in doors) d.SetLocked(false);
        }
    }
}
