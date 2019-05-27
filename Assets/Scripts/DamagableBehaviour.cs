using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;             
using System.Collections;

[System.Serializable]
public struct Damage
{
    public float ammount;
    //[HideInInspector]
    public Vector3 direction;
}
public interface PoliticsSubject
{
    int SideId();
}
public interface HealthCare 
{
    void ReceiveDamage(Damage dmg);
    void Resurrect();
    float Health01();
}
public class DamagableBehaviour : MonoBehaviour, HealthCare
{
    [Header("Health")]
    public float maxHealth = 100f;
    public GameObject explosionPrefab;
    public float explosionDestructionDelay;

    public DamageTakenEvent onDamageTaken;
    [System.Serializable]
    public class DamageTakenEvent : UnityEvent<Damage> { }

    public UnityEvent onDeath;
    
    float health;
    GameObject explosion;
    protected virtual void Awake()
    {
        Resurrect();
        explosion = Instantiate(explosionPrefab);
        explosion.transform.SetParent(transform);
        explosion.transform.localPosition = Vector3.zero;
        explosion.SetActive(false);
    }


    public void Resurrect()
    {
        health = maxHealth;
    }
    public void Die()
    {
        onDeath?.Invoke();
        explosion.transform.SetParent(null);
        Destroy(gameObject);
        explosion.transform.SetPositionAndRotation(gameObject.transform.position, gameObject.transform.rotation);
        explosion.SetActive(true);
        Destroy(explosion, explosionDestructionDelay);
    }

    public float Health01()
    {
        return health / maxHealth;
    }
    public void ReceiveDamage(Damage dmg)
    {
        if (health - dmg.ammount > 0)
        {
            health -= dmg.ammount;
            onDamageTaken?.Invoke(dmg);
        }
        else
            Die();
    }
}
