using NUnit.Framework;
using UnityEngine;
using Game.Dungeon;

public class PlacementMathTests
{
    [Test]
    public void RotateY_90_ForwardViraRight()
    {
        Vector3 r = PlacementMath.RotateY(Vector3.forward, 90);
        Assert.AreEqual(Vector3.right, r);
    }

    [Test]
    public void RotateBoundsY_90_TrocaExtentsXZ()
    {
        Bounds b = new Bounds(Vector3.zero, new Vector3(4f, 2f, 10f));
        Bounds r = PlacementMath.RotateBoundsY(b, 90);
        Assert.AreEqual(new Vector3(10f, 2f, 4f), r.size);
    }

    [Test]
    public void SolveYaw_EncontraRotacaoCorreta()
    {
        // North precisa virar South -> 180
        Assert.AreEqual(180, PlacementMath.SolveYaw(CardinalDirection.North, CardinalDirection.South));
        // East precisa virar South -> 90
        Assert.AreEqual(90, PlacementMath.SolveYaw(CardinalDirection.East, CardinalDirection.South));
    }

    [Test]
    public void PlaceRoom_AlinhaSocketDeEntradaNoSocketAberto()
    {
        // Socket aberto em (0,0,5) apontando North (+z).
        // Sala candidata tem socket de entrada em local (0,0,-5) apontando South.
        var entry = new RoomSocketData(new Vector3(0f, 0f, -5f), CardinalDirection.South);
        PlacementMath.PlaceRoom(new Vector3(0f, 0f, 5f), CardinalDirection.North, entry,
            out int yaw, out Vector3 t);

        // South já é oposto de North -> yaw 0; translação leva o socket de entrada para (0,0,5).
        Assert.AreEqual(0, yaw);
        Vector3 entryWorld = PlacementMath.RotateY(entry.LocalPosition, yaw) + t;
        Assert.AreEqual(new Vector3(0f, 0f, 5f), entryWorld);
    }

    [Test]
    public void Overlaps_ParedeCompartilhadaNaoConta()
    {
        // Duas salas 10x10 lado a lado (encostadas em x=10), margem 0.1 -> não sobrepõem.
        Bounds a = new Bounds(new Vector3(0, 0, 0), new Vector3(10, 4, 10));
        Bounds b = new Bounds(new Vector3(10, 0, 0), new Vector3(10, 4, 10));
        Assert.IsFalse(PlacementMath.Overlaps(a, b, 0.1f));
    }

    [Test]
    public void Overlaps_SobreposicaoRealConta()
    {
        Bounds a = new Bounds(new Vector3(0, 0, 0), new Vector3(10, 4, 10));
        Bounds b = new Bounds(new Vector3(3, 0, 0), new Vector3(10, 4, 10));
        Assert.IsTrue(PlacementMath.Overlaps(a, b, 0.1f));
    }
}
