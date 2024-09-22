using UnityEngine;
using System.Collections;

public class SpawnToys : MonoBehaviour {
    public GameObject objectToSpawn;        // The prefab to spawn
    public GameObject finishedToyPrefab;    // The prefab for the finished toy
    public GameObject wrappedToyPrefab;     // The prefab for the wrapped toy
    public Transform playerHand;            // The player's hand
    public float spawnRange = 0.65f;        // Range of the player to spawn the object
    public float pickupRange = 0.5f;        // Range of the player to pick up an object
    public float placeDownRange = 0.5f;     // Range of the player to place an object
    public float finishTime = 0.1f;           // Time required to finish an item
    private GameObject player;              // Reference to the player
    private GameObject spawnedToy;          // Reference to the currently spawned toy
    private GameObject toyOnTable;          // The toy on the table
    private bool isFinishing = false;       // Whether the player is currently finishing an item


    void Start() {
        // Find the player in the scene
        player = GameObject.FindGameObjectWithTag("Player");
    }

    void Update() { 
        if (Input.GetKeyDown(KeyCode.J)) {
            if (spawnedToy != null) {
                // 1. If holding a toy, try to place it on the table
                PlaceToyOnTable();
            } else {
                // 2. If not holding a toy, try to pick up a toy
                TryPickupToy();
                
                // 3. If no toy to pick up, check if near spawner and spawn a new toy
                if (spawnedToy == null && Vector3.Distance(player.transform.position, transform.position) <= spawnRange) {
                    SpawnToy();
                }
            }
        }
        
        if (Input.GetKeyDown(KeyCode.K)) {
            DropToy();
        }
        if (Input.GetKey(KeyCode.H) && toyOnTable != null && !isFinishing) {
            StartCoroutine(FinishItem());
        }
    }

    IEnumerator FinishItem() {
        RaycastHit hit;
        isFinishing = true;
        yield return new WaitForSeconds(finishTime);

        if (Physics.Raycast(player.transform.position, player.transform.forward, out hit, placeDownRange)){
            if (hit.collider.CompareTag("Table")) {
                // Iterate over the children of the table to find the toy
                GameObject toyOnThisTable = null;
                foreach (Transform child in hit.collider.transform) {
                    if (child.CompareTag("Toy")) {
                        toyOnThisTable = child.gameObject;
                        break;
                    }
                }

                // If there's a toy on this table, replace it with the finished toy
                if (toyOnThisTable != null) {
                    Vector3 position = toyOnThisTable.transform.position;
                    Destroy(toyOnThisTable); // Destroy the unfinished toy
                    GameObject finishedToy = Instantiate(finishedToyPrefab, position, Quaternion.identity);
                    finishedToy.transform.position = position + new Vector3(0, 0, 0); // Slightly above the table
                    finishedToy.transform.SetParent(hit.collider.transform); // Parent to the table
                }
            }
        }
        isFinishing = false;
    }

    IEnumerator WrapItem() {
        RaycastHit hit;
        isFinishing = true;
        yield return new WaitForSeconds(finishTime);

        if (Physics.Raycast(player.transform.position, player.transform.forward, out hit, placeDownRange)){
            if (hit.collider.CompareTag("Finished")) {
                if (toyOnTable != null) {
                    // Replace the unfinished toy with the finished toy
                    Vector3 position = toyOnTable.transform.position;
                    Destroy(toyOnTable);
                    toyOnTable = Instantiate(finishedToyPrefab, position, Quaternion.identity);
                    // Set the position of the spawned toy to the center of the table
                    Vector3 tablePosition = hit.collider.bounds.center;
                    toyOnTable.transform.position = tablePosition + new Vector3(0, 0.35f, 0); // Slightly above the table
                    toyOnTable.transform.SetParent(hit.collider.transform); // Parent to the table
                }
            }
        }
        isFinishing = false;
    }

     void TryPickupToy() {
        Collider[] hitColliders = Physics.OverlapSphere(player.transform.position, pickupRange);
        GameObject closestObject = null;
        float closestDistance = Mathf.Infinity;

        // First, check for toys on tables
        foreach (var hitCollider in hitColliders) {
            if (hitCollider.CompareTag("Table") || hitCollider.CompareTag("Spawner")) {
                // Check for toys that are children of the table
                foreach (Transform child in hitCollider.transform) {
                    if (child.CompareTag("Toy") || child.CompareTag("Finished")) {
                        float distance = Vector3.Distance(player.transform.position, child.position);
                        if (distance < closestDistance) {
                            closestDistance = distance;
                            closestObject = child.gameObject;
                        }
                    }
                }
            }
        }
        // If no toys found on tables, check for toys on the floor
        if (closestObject == null) {
            foreach (var hitCollider in hitColliders) {
                if (hitCollider.CompareTag("Toy")) {
                    float distance = Vector3.Distance(player.transform.position, hitCollider.transform.position);
                    if (distance < closestDistance) {
                        closestDistance = distance;
                        closestObject = hitCollider.gameObject;
                    }
                }
            }
        }

        if (closestObject != null) {
            PickupToy(closestObject);
        }
    }

    void PickupToy(GameObject objectToPickup) {
        spawnedToy = objectToPickup;
        spawnedToy.transform.SetParent(playerHand);
        spawnedToy.transform.position = playerHand.position;
        toyOnTable = null;
    }

    void SpawnToy() {
        // Instantiate the object and set its position and rotation to match the player's hand
        spawnedToy = Instantiate(objectToSpawn, playerHand.position, playerHand.rotation);
        spawnedToy.transform.SetParent(playerHand);
    }

    void PlaceToyOnTable() {
        RaycastHit hit;
        // Check if the player is looking at a valid table
        if (Physics.Raycast(player.transform.position, player.transform.forward, out hit, placeDownRange)){
            if (hit.collider.CompareTag("Table") || hit.collider.CompareTag("Spawner")) {
                // Check if there's already an object on the table
                if (hit.collider.transform.childCount == 0) { // Assuming only child objects are placed on the table
                    // Set the position of the spawned toy to the center of the table
                    Vector3 tablePosition = hit.collider.bounds.center;
                    spawnedToy.transform.position = tablePosition + new Vector3(0, 0.35f, 0); // Slightly above the table
                    spawnedToy.transform.SetParent(hit.collider.transform); // Parent to the table
                    toyOnTable = spawnedToy; // Keep reference to the toy on the table
                    spawnedToy = null; // Clear the reference to allow spawning again
                }
            }
        }
    }

    void DropToy() {
        if (spawnedToy != null) {
            spawnedToy.transform.SetParent(null);
            Vector3 dropPosition = new Vector3(spawnedToy.transform.position.x, 0.1f, spawnedToy.transform.position.z);
            spawnedToy.transform.position = dropPosition;
            spawnedToy = null;
        }
    }
}
