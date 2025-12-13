using UnityEngine;

public class WeaponSocket : MonoBehaviour
{
    [Header("Settings")]
    public VishnuWeaponType requiredWeapon;
    public PuzzleManager puzzleManager;

    [Header("State")]
    public VishnuWeaponItemData currentWeapon;       // Weapon currently in socket
    private GameObject currentWeaponModel;           // Reference to instantiated model

    public bool isOccupied => currentWeapon != null;

    private Collider socketCollider;

    private void Awake()
    {
        // Get collider for player interaction
        socketCollider = GetComponent<Collider>();
        if (socketCollider == null)
        {
            Debug.LogWarning("WeaponSocket requires a Collider on the same GameObject for interaction.");
        }
    }

    /// <summary>
    /// Player interacts with this socket
    /// </summary>
    public void Interact(InventorySystem inventory)
    {
        if (inventory == null) return;

        // If a weapon is already placed, pick it back
        if (currentWeapon != null)
        {
            inventory.AddItem(TakeWeapon());
        }
        else
        {
            // Place weapon from player's hand if holding a Vishnu weapon
            var weaponInHand = inventory.GetCurrentItem() as VishnuWeaponItemData;
            if (weaponInHand != null)
            {
                TryPlaceWeapon(weaponInHand);
                inventory.ConsumeCurrentItem();
            }
        }
    }

    /// <summary>
    /// Place a weapon in this socket. Returns previous weapon if any.
    /// </summary>
    public VishnuWeaponItemData TryPlaceWeapon(VishnuWeaponItemData weapon)
    {
        if (weapon == null) return null;

        // Remove existing weapon if any
        VishnuWeaponItemData previousWeapon = TakeWeapon();

        // Instantiate new weapon model
        currentWeapon = weapon;
        currentWeaponModel = Instantiate(weapon.model, transform);
        currentWeaponModel.transform.localPosition = Vector3.zero;
        currentWeaponModel.transform.localRotation = Quaternion.identity;
        currentWeaponModel.transform.localScale = weapon.model.transform.localScale;

        // Puzzle progress check
        if (weapon.weaponType == requiredWeapon && puzzleManager != null)
        {
            puzzleManager.NotifyWeaponPlaced();
        }

        Debug.Log($"{weapon.itemName} placed in socket ({requiredWeapon}). Previous: {(previousWeapon != null ? previousWeapon.itemName : "None")}");

        return previousWeapon;
    }

    /// <summary>
    /// Remove the weapon from the socket and return it
    /// </summary>
    public VishnuWeaponItemData TakeWeapon()
    {
        if (!isOccupied) return null;

        // Puzzle update if correct weapon
        if (currentWeapon.weaponType == requiredWeapon && puzzleManager != null)
        {
            puzzleManager.NotifyWeaponRemoved();
        }

        // Destroy model
        if (currentWeaponModel != null)
            Destroy(currentWeaponModel);

        VishnuWeaponItemData weaponToReturn = currentWeapon;

        currentWeapon = null;
        currentWeaponModel = null;

        return weaponToReturn;
    }
}
