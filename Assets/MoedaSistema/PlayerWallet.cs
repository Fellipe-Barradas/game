using UnityEngine;
using System;

public class PlayerWallet : MonoBehaviour
{
    [SerializeField] private int coins = 0;
    
    public int Coins => coins;
    
    public event Action<int> OnCoinsChanged;
    
    private void Start()
    {
        // dispara o evento inicial pra UI pegar o valor de partida
        OnCoinsChanged?.Invoke(coins);
    }
    
    public void AddCoins(int amount)
    {
        if (amount <= 0) return;
        coins += amount;
        OnCoinsChanged?.Invoke(coins);
    }
    
    public bool SpendCoins(int amount)
    {
        if (amount <= 0 || coins < amount) return false;
        coins -= amount;
        OnCoinsChanged?.Invoke(coins);
        return true;
    }
}