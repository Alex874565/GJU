using UnityEngine;

public class Rug : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if(other.gameObject.CompareTag("Player"))
        {
            Debug.Log("Walking on rug");
            other.gameObject.GetComponent<PlayerMovement>().WalkingSurface = Surface.Rug;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            Debug.Log("Walking on wood");
            other.gameObject.GetComponent<PlayerMovement>().WalkingSurface = Surface.Wood;
        }
    }
}