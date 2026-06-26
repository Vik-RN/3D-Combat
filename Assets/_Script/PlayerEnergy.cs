using UnityEngine;

public class PlayerEnergy : MonoBehaviour
{
    [Header("Energy Settings")]
    public float maxEnergy = 100f;
    public float currentEnergy;
    public float dashCost = 20f;
    public float regenRate = 12f; 

    void Start()
    {
        currentEnergy = maxEnergy;
    }

    void Update()
    {
        if (currentEnergy < maxEnergy)
        {
            currentEnergy += regenRate * Time.deltaTime;
            currentEnergy = Mathf.Clamp(currentEnergy, 0f, maxEnergy);
        }
    }

    public bool CanDash()
    {
        return currentEnergy >= dashCost;
    }

    public void UseDashEnergy()
    {
        currentEnergy -= dashCost;
    }
}