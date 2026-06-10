using System.Collections.Generic;
using NUnit.Framework;
using Game.Dungeon;

public class WeightedPickerTests
{
    [Test]
    public void Pesos_Iguais_RollDistribuiPorBucket()
    {
        var w = new List<float> { 1, 1, 1 };
        Assert.AreEqual(0, WeightedPicker.PickIndex(w, 0.0));
        Assert.AreEqual(1, WeightedPicker.PickIndex(w, 0.5));
        Assert.AreEqual(2, WeightedPicker.PickIndex(w, 0.9));
    }

    [Test]
    public void Peso_Zero_NuncaEscolhido()
    {
        var w = new List<float> { 0, 5, 0 };
        Assert.AreEqual(1, WeightedPicker.PickIndex(w, 0.0));
        Assert.AreEqual(1, WeightedPicker.PickIndex(w, 0.99));
    }

    [Test]
    public void Pesos_Desiguais_RespeitamProporcao()
    {
        var w = new List<float> { 3, 1 }; // fronteira em 0.75
        Assert.AreEqual(0, WeightedPicker.PickIndex(w, 0.0));
        Assert.AreEqual(0, WeightedPicker.PickIndex(w, 0.74));
        Assert.AreEqual(1, WeightedPicker.PickIndex(w, 0.8));
    }

    [Test]
    public void TodosZero_RetornaMenosUm()
    {
        Assert.AreEqual(-1, WeightedPicker.PickIndex(new List<float> { 0, 0 }, 0.5));
    }

    [Test]
    public void ListaVazia_RetornaMenosUm()
    {
        Assert.AreEqual(-1, WeightedPicker.PickIndex(new List<float>(), 0.5));
    }
}
