using UnityEngine;

public class LivingEntity : MonoBehaviour {

    public int colourMaterialIndex;
    public Species species;
    public Material material;

    // Living entity has coordinates
    public Coord coord;

    [HideInInspector]
    public int mapIndex;
    [HideInInspector]
    public Coord mapCoord;

    // bool for whether the animal is dead
    protected bool dead;

    /**
     * Initiate the LivingEntity
     * 
     * @param coord coordinates for the living entity be it an animal or planet
     */
    public virtual void Init (Coord coord) {
        this.coord = coord;
        transform.position = Environment.tileCentres[coord.x, coord.y];

        // Set material to the instance material
        var meshRenderer = transform.GetComponentInChildren<MeshRenderer> ();
        for (int i = 0; i < meshRenderer.sharedMaterials.Length; i++)
        {
            if (meshRenderer.sharedMaterials[i] == material) {
                material = meshRenderer.materials[i];
                break;
            }
        }

        if (material == null)
        {
            material = meshRenderer.materials[0];
        }
    }

    protected virtual void Die (CauseOfDeath cause) {
        if (!dead) {
            dead = true;
            Environment.RegisterDeath (this);
            Destroy (gameObject);
        }
    }
}